using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Finly.Services
{
    public static class CategoryService
    {
        public static event Action? CategoriesChanged;
        public static void RaiseChanged() => CategoriesChanged?.Invoke();

        //  Tworzy tabelę Categories, jeśli nie istnieje
        private static void EnsureCategoriesTable(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Categories(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Icon TEXT DEFAULT '📦',
    Color TEXT DEFAULT '#607D8B',
    Type TEXT DEFAULT 'Expense',
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);
";
            cmd.ExecuteNonQuery();
        }

        //  Wywoływana po zalogowaniu — dba tylko o istnienie tabeli, nie dodaje danych
        public static void BootstrapForUser(int userId)
        {
            using var conn = DatabaseService.GetOpenConnection();
            EnsureCategoriesTable(conn);
        }

        //  Używana przez ExpenseService — sprawdza, czy kategoria istnieje; jeśli nie, tworzy ją
        public static int EnsureCategoryExists(SqliteConnection conn, int userId, string categoryName)
        {
            // Sprawdź, czy istnieje
            using var check = conn.CreateCommand();
            check.CommandText = "SELECT Id FROM Categories WHERE UserId = @u AND Name = @n AND IsDeleted = 0 LIMIT 1;";
            check.Parameters.AddWithValue("@u", userId);
            check.Parameters.AddWithValue("@n", categoryName);

            var existing = check.ExecuteScalar();
            if (existing != null && existing != DBNull.Value)
                return Convert.ToInt32(existing);

            // Nie istnieje — utwórz
            using var insert = conn.CreateCommand();
            insert.CommandText = @"
INSERT INTO Categories (UserId, Name, Icon, Color, Type)
VALUES (@u, @n, '📦', '#607D8B', 'Expense');
SELECT last_insert_rowid();
";
            insert.Parameters.AddWithValue("@u", userId);
            insert.Parameters.AddWithValue("@n", categoryName);
            var newId = insert.ExecuteScalar();

            return Convert.ToInt32(newId);
        }

        //  Zwraca listę kategorii z sumą wydatków
        public static List<(string Name, string Color, decimal Total)> GetCategorySummary(int userId)
        {
            var result = new List<(string, string, decimal)>();

            using var conn = DatabaseService.GetOpenConnection();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
SELECT 
    c.Name, 
    c.Color, 
    IFNULL(SUM(e.Amount), 0) AS Total
FROM Categories c
LEFT JOIN Expenses e 
    ON e.CategoryId = c.Id AND e.UserId = c.UserId
WHERE c.UserId = @uid
GROUP BY c.Id, c.Name, c.Color
HAVING Total > 0
ORDER BY Total DESC;";
            cmd.Parameters.AddWithValue("@uid", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string name = reader["Name"].ToString() ?? "";
                string color = reader["Color"].ToString() ?? "#FFFFFF";
                decimal total = Convert.ToDecimal(reader["Total"]);
                result.Add((name, color, total));
            }

            return result;
        }
    }
}