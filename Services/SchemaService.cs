using System;
using System.Data.SQLite;

namespace Finly.Services
{
    public static class SchemaService
    {
        public static void Ensure(SQLiteConnection con)
        {
            if (con is null) throw new ArgumentNullException(nameof(con));

            // (opcjonalnie) transakcja dla spójności DDL
            using var tx = con.BeginTransaction();

            // włącz FK
            using (var fk = con.CreateCommand())
            {
                fk.CommandText = "PRAGMA foreign_keys = ON;";
                fk.ExecuteNonQuery();
            }

            // ===== Tabele (idempotentnie) =====
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Users(
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Username     TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    CreatedAt    TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Name bez UNIQUE (unikalność wymusza indeks per-user poniżej)
CREATE TABLE IF NOT EXISTS Categories(
    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
    Name   TEXT NOT NULL,
    UserId INTEGER NULL
);

CREATE TABLE IF NOT EXISTS Expenses(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Amount      REAL    NOT NULL,
    CategoryId  INTEGER NULL,
    Date        TEXT    NOT NULL,   -- ISO yyyy-MM-dd
    Description TEXT    NULL,
    UserId      INTEGER NOT NULL
);
";
                cmd.ExecuteNonQuery();
            }

            // ===== Migracja: dodaj kolumnę UserId do Categories, jeśli jej brak =====
            if (!ColumnExists(con, "Categories", "UserId"))
            {
                using var alter = con.CreateCommand();
                alter.CommandText = @"ALTER TABLE Categories ADD COLUMN UserId INTEGER NULL;";
                alter.ExecuteNonQuery();
            }

            // ===== Indeksy unikalności =====
            using (var idx = con.CreateCommand())
            {
                idx.CommandText = @"
-- login unikalny bez względu na wielkość liter
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Username_NC
    ON Users(Username COLLATE NOCASE);

-- unikalność nazwy kategorii w ramach użytkownika (NULL = global)
CREATE UNIQUE INDEX IF NOT EXISTS IX_Categories_User_Name
    ON Categories(COALESCE(UserId,0), Name);";
                idx.ExecuteNonQuery();
            }

            // ===== Seed: jeśli tabela Categories jest pusta, dodaj 3 globalne =====
            using (var check = con.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(1) FROM Categories;";
                var cnt = Convert.ToInt32(check.ExecuteScalar());
                if (cnt == 0)
                {
                    using var seed = con.CreateCommand();
                    seed.CommandText = @"
INSERT INTO Categories (Name) VALUES ('Jedzenie');
INSERT INTO Categories (Name) VALUES ('Transport');
INSERT INTO Categories (Name) VALUES ('Rachunki');";
                    seed.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }

        /// <summary>Sprawdza, czy kolumna istnieje w tabeli (SQLite).</summary>
        private static bool ColumnExists(SQLiteConnection con, string table, string column)
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
