using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Finly.Services
{
    /// <summary>
    /// Proste zarz¹dzanie u¿ytkownikami + "sesja" aktualnie zalogowanego.
    /// Oparte na Microsoft.Data.Sqlite (SQLite w .NET).
    /// </summary>
    public static class UserService
    {
        // ===== "Sesja" (kto jest zalogowany) =====
        public static int CurrentUserId { get; set; } = 0;
        public static string? CurrentUserName { get; set; }
        public static string? CurrentUserEmail { get; set; }

        /// <summary>U¿ywana przez Dashboard/Charts.</summary>
        public static int GetCurrentUserId() => CurrentUserId;

        /// <summary>Ustaw po udanym logowaniu.</summary>
        public static void SetCurrentUser(string username)
        {
            CurrentUserName = username;
            CurrentUserId = GetUserIdByUsername(username);
        }

        /// <summary>Wyczyœæ stan przy wylogowaniu.</summary>
        public static void ClearCurrentUser()
        {
            CurrentUserId = 0;
            CurrentUserName = null;
            CurrentUserEmail = null;
        }

        // Wygodny dostêp do connection stringa z DatabaseService.
        private static string ConnectionString => DatabaseService.ConnectionString;

        // ===== Publiczne API =====

        public static bool Register(string username, string passwordHash)
        {
            using var conn = DatabaseService.GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Users (Username, PasswordHash) VALUES (@u, @p);";
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", passwordHash);

            int affected = cmd.ExecuteNonQuery();
            if (affected > 0)
            {
                // Pobranie ID ostatniego dodanego u¿ytkownika
                using var lastCmd = conn.CreateCommand();
                lastCmd.CommandText = "SELECT last_insert_rowid();";
                long newId = (long)(lastCmd.ExecuteScalar() ?? 0);

                Console.WriteLine($"Zarejestrowano nowego u¿ytkownika: {username} (Id={newId})");
                return true;
            }
            return false;
        }

        public static bool IsUsernameAvailable(string username)
        {
            var u = Normalize(username);
            if (u is null) return false;

            using var con = new SqliteConnection(ConnectionString);
            con.Open();
            EnsureUsersSchema(con);

            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM Users WHERE lower(Username) = lower($u) LIMIT 1;";
            cmd.Parameters.AddWithValue("$u", u);
            var exists = cmd.ExecuteScalar();
            return exists is null || exists == DBNull.Value;
        }

        public static bool Login(string username, string password)
        {
            var u = Normalize(username);
            if (u is null || string.IsNullOrWhiteSpace(password)) return false;

            using var con = new SqliteConnection(ConnectionString);
            con.Open();
            EnsureUsersSchema(con);

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"SELECT Id, PasswordHash
                                FROM Users
                                WHERE lower(Username) = lower($u)
                                LIMIT 1;";
            cmd.Parameters.AddWithValue("$u", u);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return false;

            var id = r.GetInt32(0);
            var ph = r.GetString(1);

            if (!VerifyPassword(password, ph)) return false;

            CurrentUserId = id;
            CurrentUserName = u;
            // CurrentUserEmail — jeœli dodasz kolumnê Email, ustawisz tutaj
            return true;
        }

        public static int GetUserIdByUsername(string username)
        {
            using var conn = DatabaseService.GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id FROM Users WHERE Username = @u LIMIT 1;";
            cmd.Parameters.AddWithValue("@u", username);

            var result = cmd.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int id))
                return id;

            return 0;
        }

        /// <summary>Usuwa u¿ytkownika i powi¹zane dane (Expenses, kategorie u¿ytkownika).</summary>
        public static bool DeleteAccount(int userId)
        {
            using var con = new SqliteConnection(ConnectionString);
            con.Open();
            EnsureUsersSchema(con);

            using var tx = con.BeginTransaction();
            try
            {
                // 1) Expenses
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Expenses WHERE UserId = $id;";
                    cmd.Parameters.AddWithValue("$id", userId);
                    cmd.ExecuteNonQuery();
                }

                // 2) Kategorie u¿ytkownika (globalnych z NULL nie ruszamy)
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Categories WHERE UserId = $id;";
                    cmd.Parameters.AddWithValue("$id", userId);
                    cmd.ExecuteNonQuery();
                }

                // 3) Sam u¿ytkownik
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Users WHERE Id = $id;";
                    cmd.Parameters.AddWithValue("$id", userId);
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Nie znaleziono u¿ytkownika.");
                }

                tx.Commit();

                if (CurrentUserId == userId)
                    ClearCurrentUser();

                return true;
            }
            catch
            {
                try { tx.Rollback(); } catch { /* ignore */ }
                return false;
            }
        }

        // ===== Prywatne pomocnicze =====

        private static string? Normalize(string username)
            => string.IsNullOrWhiteSpace(username) ? null : username.Trim();

        private static string HashPassword(string password)
        {
            // Na projekt OK (SHA256). Produkcyjnie: PBKDF2/Argon2 + sól.
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string storedBase64Sha256)
            => string.Equals(HashPassword(password), storedBase64Sha256, StringComparison.Ordinal);

        /// <summary>Zapewnia tabelê Users + unikalnoœæ loginu case-insensitive.</summary>
        private static void EnsureUsersSchema(SqliteConnection con)
        {
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Users(
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    CreatedAt    TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Username_NC
    ON Users(Username COLLATE NOCASE);";
            cmd.ExecuteNonQuery();
        }
    }
}
