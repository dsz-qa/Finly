using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Aplikacja_do_sledzenia_wydatkow.Models;

namespace Aplikacja_do_sledzenia_wydatkow.Services
{
    public static class DatabaseService
    {
        public static string ConnectionString = "Data Source=budgetApp.db";

        public static SQLiteConnection GetConnection()
            => new SQLiteConnection(ConnectionString);

        /// <summary>
        /// Otwiera po³¹czenie i gwarantuje, ¿e schemat (tabele/indeksy) istnieje.
        /// </summary>
        public static SQLiteConnection OpenAndEnsureSchema()
        {
            var con = GetConnection();
            con.Open();
            SchemaService.Ensure(con); // m.in. UNIQUE INDEX na Users(Username COLLATE NOCASE)
            return con;
        }

        private static string ToIsoDate(DateTime dt) => dt.ToString("yyyy-MM-dd");

        // ================== EXPENSES ==================

        public static void AddExpense(Expense expense)
        {
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Expenses (Amount, CategoryId, Date, Description, UserId)
VALUES (@amount, @categoryId, @date, @description, @userId);";
            cmd.Parameters.AddWithValue("@amount", expense.Amount);
            cmd.Parameters.AddWithValue("@categoryId", expense.CategoryId);
            cmd.Parameters.AddWithValue("@date", ToIsoDate(expense.Date));
            cmd.Parameters.AddWithValue("@description", (object?)expense.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@userId", expense.UserId);
            cmd.ExecuteNonQuery();
        }

        public static void UpdateExpense(Expense expense)
        {
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
UPDATE Expenses
SET Amount=@amount, CategoryId=@categoryId, Date=@date, Description=@description
WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@amount", expense.Amount);
            cmd.Parameters.AddWithValue("@categoryId", expense.CategoryId);
            cmd.Parameters.AddWithValue("@date", ToIsoDate(expense.Date));
            cmd.Parameters.AddWithValue("@description", (object?)expense.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", expense.Id);
            cmd.ExecuteNonQuery();
        }

        public static void DeleteExpense(int expenseId)
        {
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Expenses WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@id", expenseId);
            cmd.ExecuteNonQuery();
        }

        public static List<Expense> GetExpensesByUser(int userId)
        {
            var list = new List<Expense>();
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT Id, Amount, Date, Description, CategoryId, UserId
FROM Expenses
WHERE UserId=@userId
ORDER BY Date DESC, Id DESC;";
            cmd.Parameters.AddWithValue("@userId", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Expense
                {
                    Id = r.GetInt32(0),
                    Amount = r.GetDouble(1),
                    Date = DateTime.Parse(r.GetString(2)),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    CategoryId = r.IsDBNull(4) ? 0 : r.GetInt32(4),
                    UserId = r.GetInt32(5)
                });
            }
            return list;
        }

        public static Expense? GetExpenseById(int expenseId)
        {
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT Id, Amount, Date, Description, CategoryId, UserId
FROM Expenses
WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@id", expenseId);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new Expense
            {
                Id = r.GetInt32(0),
                Amount = r.GetDouble(1),
                Date = DateTime.Parse(r.GetString(2)),
                Description = r.IsDBNull(3) ? "" : r.GetString(3),
                CategoryId = r.IsDBNull(4) ? 0 : r.GetInt32(4),
                UserId = r.GetInt32(5)
            };
        }

        public static List<Expense> GetExpensesByUserId(int userId)
        {
            var list = new List<Expense>();
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT e.Id, e.Amount, e.Date, e.Description, e.CategoryId, c.Name
FROM Expenses e
LEFT JOIN Categories c ON e.CategoryId = c.Id
WHERE e.UserId=@userId
ORDER BY e.Date DESC, e.Id DESC;";
            cmd.Parameters.AddWithValue("@userId", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Expense
                {
                    Id = r.GetInt32(0),
                    Amount = r.GetDouble(1),
                    Date = DateTime.Parse(r.GetString(2)),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    CategoryId = r.IsDBNull(4) ? 0 : r.GetInt32(4),
                    CategoryName = r.IsDBNull(5) ? "Brak kategorii" : r.GetString(5),
                    UserId = userId
                });
            }
            return list;
        }

        public static List<ExpenseDisplayModel> GetExpensesWithCategory()
        {
            var list = new List<ExpenseDisplayModel>();
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT e.Id, e.Amount, e.Date, e.Description, e.UserId, c.Name as CategoryName
FROM Expenses e
LEFT JOIN Categories c ON e.CategoryId = c.Id
ORDER BY e.Date DESC, e.Id DESC;";

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ExpenseDisplayModel
                {
                    Id = r.GetInt32(0),
                    Amount = r.GetDouble(1),
                    Date = DateTime.Parse(r.GetString(2)),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    UserId = r.GetInt32(4),
                    CategoryName = r.IsDBNull(5) ? "Brak kategorii" : r.GetString(5)
                });
            }
            return list;
        }

        public static List<ExpenseDisplayModel> GetExpensesWithCategoryNameByUser(int userId)
        {
            var list = new List<ExpenseDisplayModel>();
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT e.Id, e.Amount, e.Date, e.Description, c.Name as CategoryName
FROM Expenses e
LEFT JOIN Categories c ON e.CategoryId = c.Id
WHERE e.UserId=@userId
ORDER BY e.Date DESC, e.Id DESC;";
            cmd.Parameters.AddWithValue("@userId", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ExpenseDisplayModel
                {
                    Id = r.GetInt32(0),
                    Amount = r.GetDouble(1),
                    Date = DateTime.Parse(r.GetString(2)),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    CategoryName = r.IsDBNull(4) ? "Brak kategorii" : r.GetString(4),
                    UserId = userId
                });
            }
            return list;
        }

        // ================== CATEGORIES ==================

        /// <summary> Zwróæ nazwê kategorii po Id (null, jeœli brak) </summary>
        public static string? GetCategoryNameById(int categoryId)
        {
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT Name FROM Categories WHERE Id=@id LIMIT 1;";
            cmd.Parameters.AddWithValue("@id", categoryId);
            var res = cmd.ExecuteScalar();
            return res == null || res == DBNull.Value ? null : Convert.ToString(res);
        }

        /// <summary> Spróbuj znaleŸæ Id kategorii po nazwie: najpierw per-user, potem global. </summary>
        public static int? TryGetCategoryIdByName(string categoryName, int userId)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return null;
            using var connection = OpenAndEnsureSchema();

            // per-user
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"SELECT Id FROM Categories
                                    WHERE Name=@name AND COALESCE(UserId,0)=@userId
                                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@name", categoryName.Trim());
                cmd.Parameters.AddWithValue("@userId", userId);
                var r = cmd.ExecuteScalar();
                if (r != null && r != DBNull.Value)
                    return Convert.ToInt32(r);
            }

            // globalnie
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"SELECT Id FROM Categories
                                    WHERE Name=@name
                                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@name", categoryName.Trim());
                var r = cmd.ExecuteScalar();
                if (r != null && r != DBNull.Value)
                    return Convert.ToInt32(r);
            }

            return null;
        }

        /// <summary> Zwróæ Id istniej¹cej kategorii albo utwórz i zwróæ nowe Id. </summary>
        public static int GetOrCreateCategoryId(string categoryName, int userId)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                categoryName = "Inne";

            using var connection = OpenAndEnsureSchema();

            // 1) per-user (jeœli istnieje)
            using (var checkUser = connection.CreateCommand())
            {
                checkUser.CommandText = @"
SELECT Id FROM Categories
WHERE Name=@name AND COALESCE(UserId,0)=@userId
LIMIT 1;";
                checkUser.Parameters.AddWithValue("@name", categoryName.Trim());
                checkUser.Parameters.AddWithValue("@userId", userId);
                var res = checkUser.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                    return Convert.ToInt32(res);
            }

            // 2) globalnie
            using (var checkGlobal = connection.CreateCommand())
            {
                checkGlobal.CommandText = @"
SELECT Id FROM Categories
WHERE Name=@name
LIMIT 1;";
                checkGlobal.Parameters.AddWithValue("@name", categoryName.Trim());
                var res = checkGlobal.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                    return Convert.ToInt32(res);
            }

            // 3) utwórz (bez wyj¹tku UNIQUE)
            using (var insert = connection.CreateCommand())
            {
                insert.CommandText = @"
INSERT OR IGNORE INTO Categories (Name, UserId)
VALUES (@name, @userId);";
                insert.Parameters.AddWithValue("@name", categoryName.Trim());
                insert.Parameters.AddWithValue("@userId", userId);
                insert.ExecuteNonQuery();
            }

            using (var getId = connection.CreateCommand())
            {
                getId.CommandText = @"SELECT Id FROM Categories WHERE Name=@name LIMIT 1;";
                getId.Parameters.AddWithValue("@name", categoryName.Trim());
                return Convert.ToInt32(getId.ExecuteScalar());
            }
        }

        /// <summary> Kategorie dostêpne dla usera (jego + globalne, bez duplikatów). </summary>
        public static List<string> GetCategoriesByUser(int userId)
        {
            var list = new List<string>();
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
SELECT DISTINCT Name
FROM Categories
WHERE COALESCE(UserId,0)=@userId OR UserId IS NULL
ORDER BY Name;";
            cmd.Parameters.AddWithValue("@userId", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(r.GetString(0));

            return list;
        }
    }
}
