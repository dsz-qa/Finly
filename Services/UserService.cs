using Finly.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Finly.Services
{
    /// <summary>
    /// Zarządzanie użytkownikami + stan „kto zalogowany”.
    /// </summary>
    public static class UserService
    {
        // ===== Stan logowania =====
        public static int CurrentUserId { get; set; } = 0;
        public static string? CurrentUserName { get; set; }
        public static string? CurrentUserEmail { get; set; }

        public static int GetCurrentUserId() => CurrentUserId;

        public static void SetCurrentUser(string username)
        {
            CurrentUserName = username;
            CurrentUserId = GetUserIdByUsername(username);
            CurrentUserEmail = GetEmail(CurrentUserId);
        }

        public static void ClearCurrentUser()
        {
            CurrentUserId = 0;
            CurrentUserName = null;
            CurrentUserEmail = null;
        }

        // ===== Typ konta =====
        public static AccountType GetAccountType(int userId)
        {
            using var con = DatabaseService.GetConnection();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT AccountType FROM Users WHERE Id=@id LIMIT 1;";
            cmd.Parameters.AddWithValue("@id", userId);
            var raw = cmd.ExecuteScalar()?.ToString();

            return string.Equals(raw, "Business", StringComparison.OrdinalIgnoreCase)
                ? AccountType.Business
                : AccountType.Personal;
        }

        // ===== Rejestracja / logowanie =====

        // Uproszczona wersja – zachowana kompatybilność
        public static bool Register(string username, string password)
            => Register(username, password, AccountType.Personal, null, null, null, null, null);

        // Pełna wersja rejestracji z typem konta i danymi firmy
        public static bool Register(
            string username,
            string password,
            AccountType accountType,
            string? companyName,
            string? nip,
            string? regon,
            string? krs,
            string? companyAddress)
        {
            var u = Normalize(username);
            if (u is null || string.IsNullOrWhiteSpace(password)) return false;

            using var con = DatabaseService.GetConnection();
            SchemaService.Ensure(con); // upewniamy się, że tabele istnieją

            // Czy login już istnieje?
            using (var check = con.CreateCommand())
            {
                check.CommandText = "SELECT 1 FROM Users WHERE lower(Username)=lower($u) LIMIT 1;";
                check.Parameters.AddWithValue("$u", u);
                var exists = check.ExecuteScalar();
                if (exists != null && exists != DBNull.Value)
                    return false;
            }

            using (var ins = con.CreateCommand())
            {
                ins.CommandText = @"
INSERT INTO Users (Username, PasswordHash, AccountType, CompanyName, NIP, REGON, KRS, CompanyAddress)
VALUES ($u, $ph, $type, $cname, $nip, $regon, $krs, $caddr);";
                ins.Parameters.AddWithValue("$u", u);
                ins.Parameters.AddWithValue("$ph", HashPassword(password));
                ins.Parameters.AddWithValue("$type", accountType == AccountType.Business ? "Business" : "Personal");
                ins.Parameters.AddWithValue("$cname", (object?)companyName ?? DBNull.Value);
                ins.Parameters.AddWithValue("$nip", (object?)nip ?? DBNull.Value);
                ins.Parameters.AddWithValue("$regon", (object?)regon ?? DBNull.Value);
                ins.Parameters.AddWithValue("$krs", (object?)krs ?? DBNull.Value);
                ins.Parameters.AddWithValue("$caddr", (object?)companyAddress ?? DBNull.Value);
                return ins.ExecuteNonQuery() == 1;
            }
        }

        public static bool IsUsernameAvailable(string username)
        {
            var u = Normalize(username);
            if (u is null) return false;

            using var con = DatabaseService.GetConnection();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM Users WHERE lower(Username)=lower($u) LIMIT 1;";
            cmd.Parameters.AddWithValue("$u", u);
            var exists = cmd.ExecuteScalar();
            return exists is null || exists == DBNull.Value;
        }

        public static bool Login(string username, string password)
        {
            var u = Normalize(username);
            if (u is null || string.IsNullOrWhiteSpace(password)) return false;

            using var con = DatabaseService.GetConnection();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"SELECT Id, PasswordHash FROM Users
                                WHERE lower(Username)=lower($u) LIMIT 1;";
            cmd.Parameters.AddWithValue("$u", u);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return false;

            var id = r.GetInt32(0);
            var ph = r.GetString(1);
            if (!VerifyPassword(password, ph)) return false;

            CurrentUserId = id;
            CurrentUserName = u;
            CurrentUserEmail = GetEmail(id);
            return true;
        }

        public static int GetUserIdByUsername(string username)
        {
            var u = Normalize(username) ?? string.Empty;
            using var con = DatabaseService.GetConnection();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id FROM Users WHERE lower(Username)=lower($u) LIMIT 1;";
            cmd.Parameters.AddWithValue("$u", u);
            var obj = cmd.ExecuteScalar();
            return (obj is null || obj == DBNull.Value) ? 0 : Convert.ToInt32(obj);
        }

        // ===== Zmiana hasła =====
        public static bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            using var c = DatabaseService.GetConnection();

            string currentHash = "";
            using (var get = c.CreateCommand())
            {
                get.CommandText = "SELECT PasswordHash FROM Users WHERE Id=@id;";
                get.Parameters.AddWithValue("@id", userId);
                currentHash = get.ExecuteScalar()?.ToString() ?? "";
            }

            if (!string.Equals(currentHash, HashPassword(oldPassword), StringComparison.Ordinal))
                return false;

            using (var upd = c.CreateCommand())
            {
                upd.CommandText = "UPDATE Users SET PasswordHash=@h WHERE Id=@id;";
                upd.Parameters.AddWithValue("@h", HashPassword(newPassword));
                upd.Parameters.AddWithValue("@id", userId);
                upd.ExecuteNonQuery();
            }
            return true;
        }

        // ===== Dane konta =====
        public static string GetUsername(int userId) => GetUserById(userId)?.Username ?? "";
        public static string GetEmail(int userId) => GetUserById(userId)?.Email ?? "";
        public static void UpdateEmail(int userId, string email)
        {
            using var c = DatabaseService.GetConnection();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Users SET Email=@e WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@e", email ?? "");
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();
        }

        public static DateTime GetCreatedAt(int userId)
            => GetUserById(userId)?.CreatedAt ?? DateTime.MinValue;

        public static (int Id, string Username, string? Email, DateTime CreatedAt)? GetUserById(int userId)
        {
            using var c = DatabaseService.GetConnection();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Email, CreatedAt FROM Users WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@id", userId);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            DateTime createdAt;
            var raw = r.GetValue(3);
            if (raw is DateTime dt) createdAt = dt;
            else if (DateTime.TryParse(raw?.ToString(), out var parsed)) createdAt = parsed;
            else createdAt = DateTime.MinValue;

            return (r.GetInt32(0),
                    r.GetString(1),
                    r.IsDBNull(2) ? null : r.GetString(2),
                    createdAt);
        }

        // ===== Usuwanie konta =====
        public static bool DeleteAccount(int userId)
        {
            try
            {
                using var con = DatabaseService.GetConnection();
                using var tx = con.BeginTransaction();

                void Exec(string sql)
                {
                    using var cmd = con.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }

                Exec("DELETE FROM Expenses        WHERE UserId=@id;");
                Exec("DELETE FROM Categories      WHERE UserId=@id;");
                Exec("DELETE FROM BankAccounts    WHERE UserId=@id;");
                Exec("DELETE FROM BankConnections WHERE UserId=@id;");
                Exec("DELETE FROM PersonalProfiles WHERE UserId=@id;");
                Exec("DELETE FROM CompanyProfiles  WHERE UserId=@id;");

                using (var del = con.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM Users WHERE Id=@id;";
                    del.Parameters.AddWithValue("@id", userId);
                    if (del.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Nie znaleziono użytkownika.");
                }

                tx.Commit();

                if (CurrentUserId == userId) ClearCurrentUser();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ===== Profil użytkownika =====
        public static UserProfile GetProfile(int userId)
        {
            using var c = DatabaseService.GetConnection();

            string? firstName = null, lastName = null, address = null;
            string? birthYear = null, city = null, postalCode = null, houseNo = null;

            try
            {
                using var p = c.CreateCommand();
                p.CommandText = @"
SELECT FirstName, LastName, Address, BirthDate, City, PostalCode, HouseNo
FROM PersonalProfiles WHERE UserId=@id LIMIT 1;";
                p.Parameters.AddWithValue("@id", userId);

                using var pr = p.ExecuteReader();
                if (pr.Read())
                {
                    firstName = pr.IsDBNull(0) ? null : pr.GetString(0);
                    lastName = pr.IsDBNull(1) ? null : pr.GetString(1);
                    address = pr.IsDBNull(2) ? null : pr.GetString(2);
                    if (!pr.IsDBNull(3))
                    {
                        var d = pr.GetString(3);
                        if (DateTime.TryParse(d, out var bd)) birthYear = bd.Year.ToString();
                    }
                    city = pr.IsDBNull(4) ? null : pr.GetString(4);
                    postalCode = pr.IsDBNull(5) ? null : pr.GetString(5);
                    houseNo = pr.IsDBNull(6) ? null : pr.GetString(6);
                }
            }
            catch { }

            string? companyName = null, companyNip = null, companyAddress = null;
            using (var u = c.CreateCommand())
            {
                u.CommandText = @"
SELECT FirstName, LastName, Address,
       CompanyName,
       COALESCE(NIP, CompanyNip) as NipCompat,
       CompanyAddress
FROM Users WHERE Id=@id;";
                u.Parameters.AddWithValue("@id", userId);
                using var ur = u.ExecuteReader();
                if (ur.Read())
                {
                    firstName ??= ur.IsDBNull(0) ? null : ur.GetString(0);
                    lastName ??= ur.IsDBNull(1) ? null : ur.GetString(1);
                    address ??= ur.IsDBNull(2) ? null : ur.GetString(2);

                    companyName = ur.IsDBNull(3) ? null : ur.GetString(3);
                    companyNip = ur.IsDBNull(4) ? null : ur.GetString(4);
                    companyAddress = ur.IsDBNull(5) ? null : ur.GetString(5);
                }
            }

            return new UserProfile
            {
                FirstName = firstName,
                LastName = lastName,
                Address = address,
                BirthYear = birthYear,
                City = city,
                PostalCode = postalCode,
                HouseNo = houseNo,
                CompanyName = companyName,
                CompanyNip = companyNip,
                CompanyAddress = companyAddress
            };
        }

        public static void UpdateProfile(int userId, UserProfile p)
        {
            using var c = DatabaseService.GetConnection();

            try
            {
                using (var up = c.CreateCommand())
                {
                    up.CommandText = @"
INSERT INTO PersonalProfiles (UserId, FirstName, LastName, Address, City, PostalCode, HouseNo, CreatedAt)
VALUES (@id, @fn, @ln, @addr, @city, @pc, @house, CURRENT_TIMESTAMP)
ON CONFLICT(UserId) DO UPDATE SET
    FirstName = @fn,
    LastName  = @ln,
    Address   = @addr,
    City      = @city,
    PostalCode= @pc,
    HouseNo   = @house;";
                    up.Parameters.AddWithValue("@id", userId);
                    up.Parameters.AddWithValue("@fn", (object?)p.FirstName ?? DBNull.Value);
                    up.Parameters.AddWithValue("@ln", (object?)p.LastName ?? DBNull.Value);
                    up.Parameters.AddWithValue("@addr", (object?)p.Address ?? DBNull.Value);
                    up.Parameters.AddWithValue("@city", (object?)p.City ?? DBNull.Value);
                    up.Parameters.AddWithValue("@pc", (object?)p.PostalCode ?? DBNull.Value);
                    up.Parameters.AddWithValue("@house", (object?)p.HouseNo ?? DBNull.Value);
                    up.ExecuteNonQuery();
                }

                if (int.TryParse(p.BirthYear, out var y) && y >= 1900 && y <= DateTime.Now.Year)
                {
                    using var upBirth = c.CreateCommand();
                    upBirth.CommandText = @"
UPDATE PersonalProfiles
SET BirthDate = date(@iso,'start of year')
WHERE UserId=@id;";
                    upBirth.Parameters.AddWithValue("@iso", $"{y}-01-01");
                    upBirth.Parameters.AddWithValue("@id", userId);
                    upBirth.ExecuteNonQuery();
                }
            }
            catch
            {
                using var upu = c.CreateCommand();
                upu.CommandText = @"
UPDATE Users SET
    FirstName = @fn,
    LastName  = @ln,
    Address   = @addr
WHERE Id=@id;";
                upu.Parameters.AddWithValue("@fn", (object?)p.FirstName ?? DBNull.Value);
                upu.Parameters.AddWithValue("@ln", (object?)p.LastName ?? DBNull.Value);
                upu.Parameters.AddWithValue("@addr", (object?)p.Address ?? DBNull.Value);
                upu.Parameters.AddWithValue("@id", userId);
                upu.ExecuteNonQuery();
            }

            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE Users SET
    CompanyName    = @cname,
    NIP            = @nip,
    CompanyAddress = @caddr
WHERE Id=@id;";
                cmd.Parameters.AddWithValue("@cname", (object?)p.CompanyName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@nip", (object?)p.CompanyNip ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@caddr", (object?)p.CompanyAddress ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
        }

        // ===== Pomocnicze =====
        private static string? Normalize(string username)
            => string.IsNullOrWhiteSpace(username) ? null : username.Trim();

        private static string HashPassword(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? ""));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string storedBase64Sha256)
            => string.Equals(HashPassword(password), storedBase64Sha256, StringComparison.Ordinal);
    }
}