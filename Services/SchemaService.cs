using System;
using Microsoft.Data.Sqlite;

namespace Finly.Services
{
    public static class SchemaService
    {
        public static void Ensure(SqliteConnection con)
        {
            if (con is null) throw new ArgumentNullException(nameof(con));

            using var tx = con.BeginTransaction();

            // PRAGMA
            using (var fk = con.CreateCommand())
            {
                fk.CommandText = "PRAGMA foreign_keys = ON;";
                fk.ExecuteNonQuery();
            }

            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Users(
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    CreatedAt    TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Expenses(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    CategoryId INTEGER NOT NULL,
    Amount REAL NOT NULL,
    Date TEXT NOT NULL,
    Description TEXT
);

CREATE TABLE IF NOT EXISTS Expenses(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Amount      REAL    NOT NULL,
    CategoryId  INTEGER NULL,
    Date        TEXT    NOT NULL,
    Description TEXT    NULL,
    UserId      INTEGER NOT NULL
);";
                cmd.ExecuteNonQuery();
            }

            // Kolumna UserId (dla starszych baz)
            if (!ColumnExists(con, "Categories", "UserId"))
            {
                using var alter = con.CreateCommand();
                alter.CommandText = @"ALTER TABLE Categories ADD COLUMN UserId INTEGER NULL;";
                alter.ExecuteNonQuery();
            }

            using (var idx = con.CreateCommand())
            {
                idx.CommandText = @"
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Username_NC
    ON Users(Username COLLATE NOCASE);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Categories_User_Name
    ON Categories(COALESCE(UserId,0), Name);";
                idx.ExecuteNonQuery();
            }

            // 🟢 Nie dodajemy już domyślnych kategorii
            // (każdy użytkownik tworzy je sam po dodaniu pierwszego wydatku)

            tx.Commit();
        }

        private static bool ColumnExists(SqliteConnection con, string table, string column)
        {
            var safeTable = table.Replace("'", "''");
            using var cmd = con.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info('{safeTable}');";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var colName = r["name"]?.ToString();
                if (string.Equals(colName, column, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}