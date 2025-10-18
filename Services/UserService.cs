using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;

namespace Finly.Services
{
    public static class UserService
    {

        public static int CurrentUserId { get; set; } = 0;
        public static int GetCurrentUserId() => CurrentUserId;
        // ========== PUBLIC API ==========

        public static bool Register(string username, string password)
        {
            var normalized = Normalize(username);
            if (normalized is null || string.IsNullOrWhiteSpace(password))
                return false;

            var passwordHash = HashPassword(password);

            using var connection = DatabaseService.OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Users (Username, PasswordHash)
VALUES (@username, @passwordHash);";
            cmd.Parameters.AddWithValue("@username", normalized);
            cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

            try
            {
                return cmd.ExecuteNonQuery() == 1;
            }
            catch (SQLiteException)
            {
                // np. naruszenie unikalnoœci loginu (index COLLATE NOCASE)
                return false;
            }
        }

        public static bool IsUsernameAvailable(string username)
        {
            var normalized = Normalize(username);
            if (normalized is null) return false;

            using var connection = DatabaseService.OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT 1
FROM Users
WHERE Username = @username COLLATE NOCASE
LIMIT 1;";
            cmd.Parameters.AddWithValue("@username", normalized);

            var exists = cmd.ExecuteScalar();
            // true = wolny (brak wiersza)
            return exists is null || exists == DBNull.Value;
        }

        public static bool Login(string username, string password)
        {
            var normalized = Normalize(username);
            if (normalized is null || string.IsNullOrWhiteSpace(password))
                return false;

            using var connection = DatabaseService.OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT PasswordHash
FROM Users
WHERE Username = @username COLLATE NOCASE;";
            cmd.Parameters.AddWithValue("@username", normalized);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return false;

            var storedHash = reader.GetString(0);
            return VerifyPassword(password, storedHash);
        }

        public static int GetUserIdByUsername(string username)
        {
            var normalized = Normalize(username) ?? string.Empty;

            using var connection = DatabaseService.OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT Id
FROM Users
WHERE Username = @username COLLATE NOCASE;";
            cmd.Parameters.AddWithValue("@username", normalized);

            var result = cmd.ExecuteScalar();
            return (result is null || result == DBNull.Value) ? -1 : Convert.ToInt32(result);
        }

        public static bool DeleteAccount(int userId)
        {
            using var con = DatabaseService.OpenAndEnsureSchema();
            using var tx = con.BeginTransaction();

            try
            {
                // 1) Usuñ wydatki u¿ytkownika
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Expenses WHERE UserId = @id;";
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }

                // 2) Usuñ kategorie przypisane do u¿ytkownika (globalnych z NULL nie ruszamy)
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Categories WHERE UserId = @id;";
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }

                // 3) Usuñ u¿ytkownika
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Users WHERE Id = @id;";
                    cmd.Parameters.AddWithValue("@id", userId);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows != 1)
                        throw new InvalidOperationException("Nie znaleziono u¿ytkownika do usuniêcia.");
                }

                tx.Commit();
                return true;
            }
            catch
            {
                try { tx.Rollback(); } catch { /* ignorujemy */ }
                return false;
            }
        }

        // ========== INTERNALS ==========

        private static string? Normalize(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            return username.Trim().ToLowerInvariant();
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var incoming = HashPassword(password);
            return string.Equals(incoming, storedHash, StringComparison.Ordinal);
        }
    }
}
