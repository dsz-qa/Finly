using Finly.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Finly.Services
{
    /// Proste zarz¹dzanie u¿ytkownikami + stan „kto zalogowany”.
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

        // ===== Rejestracja / logowanie =====

        public static bool Register(string username, string password)
        {
            var u = Normalize(username);
            if (u is null || string.IsNullOrWhiteSpace(password)) return false;

            using var con = DatabaseService.GetConnection();

            // Czy login wolny?
            using (var check = con.CreateCommand())
            {
                check.CommandText = "SELECT 1 FROM Users WHERE lower(Username)=lower($u) LIMIT 1;";
                check.Parameters.AddWithValue("$u", u);
                var exists = check.ExecuteScalar();
                if (exists != null && exists != DBNull.Value) return false;
            }

            // Wstaw
            using (var ins = con.CreateCommand())
            {
                ins.CommandText = @"INSERT INTO Users (Username, PasswordHash) VALUES ($u, $ph);";
                ins.Parameters.AddWithValue("$u", u);
                ins.Parameters.AddWithValue("$ph", HashPassword(password));
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
            return (obj is null || obj == DBNull.Value) ? -1 : Convert.ToInt32(obj);
        }

        /// Zmiana has³a (sprawdza stare).
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

        // ===== Pobieranie profilu =====

        public static string GetUsername(int userId) => GetUserById(userId)?.Username ?? "";
        public static string GetEmail(int userId) => GetUserById(userId)?.Email ?? "";
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

            // CreatedAt to TEXT – parsowanie bezpieczne
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

        /// Idempotentna definicja Users, jeœli chcia³abyœ wo³aæ niezale¿nie.
        private static void EnsureUsersSchema(SqliteConnection con)
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Users(
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Email        TEXT NULL,
    CreatedAt    TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Username_NC
    ON Users(Username COLLATE NOCASE);";
            cmd.ExecuteNonQuery();
        }

        /// Usuwa konto u¿ytkownika i wszystkie jego dane (wydatki, kategorie, banki).
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

                Exec("DELETE FROM Expenses       WHERE UserId=@id;");
                Exec("DELETE FROM Categories     WHERE UserId=@id;");
                Exec("DELETE FROM BankAccounts   WHERE UserId=@id;");
                Exec("DELETE FROM BankConnections WHERE UserId=@id;");

                using (var del = con.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "DELETE FROM Users WHERE Id=@id;";
                    del.Parameters.AddWithValue("@id", userId);
                    if (del.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Nie znaleziono u¿ytkownika.");
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


        public static UserProfile GetProfile(int userId)
        {
            using var c = DatabaseService.GetConnection();
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT FirstName, LastName, Address,
                               CompanyName, CompanyNip, CompanyAddress,
                               Email
                        FROM Users WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@id", userId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return new UserProfile();

            return new UserProfile
            {
                FirstName = r.IsDBNull(0) ? null : r.GetString(0),
                LastName = r.IsDBNull(1) ? null : r.GetString(1),
                Address = r.IsDBNull(2) ? null : r.GetString(2),
                CompanyName = r.IsDBNull(3) ? null : r.GetString(3),
                CompanyNip = r.IsDBNull(4) ? null : r.GetString(4),
                CompanyAddress = r.IsDBNull(5) ? null : r.GetString(5),
                // Email mamy te¿ osobno, ale niech siê wype³ni, jeœli chcesz u¿ywaæ w UI:
                // mo¿esz dodaæ do modelu, jeœli przyda siê do edycji
            };
        }

        public static void UpdateProfile(int userId, UserProfile p)
        {
            using var c = DatabaseService.GetConnection();
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"
UPDATE Users SET
    FirstName      = @fn,
    LastName       = @ln,
    Address        = @addr,
    CompanyName    = @cname,
    CompanyNip     = @cnip,
    CompanyAddress = @caddr
WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@fn", (object?)p.FirstName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ln", (object?)p.LastName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@addr", (object?)p.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cname", (object?)p.CompanyName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cnip", (object?)p.CompanyNip ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caddr", (object?)p.CompanyAddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();
        }
    }
}


