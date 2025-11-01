using System;
using Microsoft.Data.Sqlite;

namespace Finly.Services
{
    public static class ExpenseService
    {
        public static void AddExpense(int userId, string categoryName, decimal amount, string description, DateTime date)
        {
            using var conn = DatabaseService.GetOpenConnection();
            using var tx = conn.BeginTransaction();

            // Upewnij się, że kategoria istnieje lub ją utwórz
            int categoryId = CategoryService.EnsureCategoryExists(conn, userId, categoryName);

            // Zapisz wydatek
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
INSERT INTO Expenses (UserId, CategoryId, Amount, Date, Description)
VALUES (@u, @c, @a, @d, @desc);
";
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@c", categoryId);
            cmd.Parameters.AddWithValue("@a", amount);
            cmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@desc", description ?? "");
            cmd.ExecuteNonQuery();

            tx.Commit();
            CategoryService.RaiseChanged();
        }
    }
}