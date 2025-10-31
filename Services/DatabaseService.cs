using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Finly.Models;

namespace Finly.Services
{
    public static class DatabaseService
    {
        /// <summary>
        /// Wo³aj po zalogowaniu: tworzy wymagane tabele (bez seedów).
        /// </summary>
        public static void EnsureCoreTables()
        {
            using var conn = GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Users(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Categories(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Icon TEXT NOT NULL DEFAULT ' ',
    Color TEXT NOT NULL DEFAULT '#607D8B',
    Type TEXT NOT NULL CHECK(Type IN ('Expense','Income','Saving')) DEFAULT 'Expense',
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE(UserId, Name) WHERE IsDeleted = 0,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Expenses(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    CategoryId INTEGER NOT NULL,
    Amount REAL NOT NULL,
    Description TEXT,
    Date TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY(CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
);
";
            cmd.ExecuteNonQuery();
        }
        // Lokalna baza w katalogu aplikacji
        public static string ConnectionString = "Data Source=budgetApp.db";

        public static SqliteConnection GetConnection()
            => new SqliteConnection(ConnectionString);

        public static SqliteConnection OpenAndEnsureSchema()
        {
            var con = GetConnection();
            con.Open();
            SchemaService.Ensure(con);
            return con;
        }

        private static string ToIsoDate(DateTime dt) => dt.ToString("yyyy-MM-dd");

        // ---------- EXPENSES ----------
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
                    CategoryName = r.IsDBNull(5) ? "Brak kategorii" : r.GetString(5),
                    Category = r.IsDBNull(5) ? "Brak kategorii" : r.GetString(5)
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
                    Category = r.IsDBNull(4) ? "Brak kategorii" : r.GetString(4),
                    UserId = userId
                });
            }
            return list;
        }

        // ---------- CATEGORIES ----------
        public static string? GetCategoryNameById(int categoryId)
        {
            using var connection = OpenAndEnsureSchema();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT Name FROM Categories WHERE Id=@id LIMIT 1;";
            cmd.Parameters.AddWithValue("@id", categoryId);
            var res = cmd.ExecuteScalar();
            return res == null || res == DBNull.Value ? null : Convert.ToString(res);
        }

        public static int? TryGetCategoryIdByName(string categoryName, int userId)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return null;
            using var connection = OpenAndEnsureSchema();

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

        public static int GetOrCreateCategoryId(string categoryName, int userId)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                categoryName = "Inne";

            using var connection = OpenAndEnsureSchema();

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

        public static SqliteConnection GetOpenConnection()
        {
            var conn = new SqliteConnection(DatabaseService.ConnectionString);
            conn.Open();
            return conn;
        }

        public static void EnsureTables()
        {
            using var conn = GetOpenConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Categories(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Icon TEXT NOT NULL,
    Color TEXT NOT NULL,
    Type TEXT NOT NULL CHECK(Type IN ('Expense','Income','Saving')),
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Categories_User_Name 
ON Categories(UserId, Name) WHERE IsDeleted = 0;
";
            cmd.ExecuteNonQuery();
        }
    }
}

