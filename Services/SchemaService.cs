using System;
using Microsoft.Data.Sqlite;

namespace Finly.Services
{
    public static class SchemaService
    {
        // Chroni przed równoległymi migracjami i „database is locked”
        private static readonly object _schemaLock = new();

        /// <summary>
        /// Gwarantuje istnienie schematu bazy i wykonuje brakujące migracje.
        /// Bezpieczna przy wielokrotnym wywołaniu.
        /// </summary>
        public static void Ensure(SqliteConnection con)
        {
            if (con is null) throw new ArgumentNullException(nameof(con));

            lock (_schemaLock)
            {
                // Ustawienia runtime (poza transakcją)
                using (var p = con.CreateCommand())
                {
                    p.CommandText = @"PRAGMA busy_timeout = 5000;
                                      PRAGMA journal_mode = WAL;";
                    p.ExecuteNonQuery();
                }

                using var tx = con.BeginTransaction();

                // Helper do komend w obrębie tej transakcji
                SqliteCommand Cmd(string sql)
                {
                    var c = con.CreateCommand();
                    c.Transaction = tx;
                    c.CommandText = sql;
                    return c;
                }

                // Helper do sprawdzania kolumn
                bool Col(string table, string col) => ColumnExists(con, tx, table, col);

                // ---------- Tabele (IF NOT EXISTS) ----------
                using (var cmd = Cmd(@"
CREATE TABLE IF NOT EXISTS Users(
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Username        TEXT NOT NULL UNIQUE,
    PasswordHash    TEXT NOT NULL,
    Email           TEXT NULL,
    FirstName       TEXT NULL,
    LastName        TEXT NULL,
    Address         TEXT NULL,
    CompanyName     TEXT NULL,
    CompanyNip      TEXT NULL,
    CompanyAddress  TEXT NULL,
    CreatedAt       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Categories(
    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
    Name   TEXT NOT NULL,
    UserId INTEGER NULL
);

CREATE TABLE IF NOT EXISTS Expenses(
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Amount      REAL    NOT NULL,
    CategoryId  INTEGER NULL,
    Date        TEXT    NOT NULL,
    Description TEXT    NULL,
    UserId      INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS BankConnections(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId        INTEGER NOT NULL,
    BankName      TEXT NOT NULL,
    AccountHolder TEXT NOT NULL,
    Status        TEXT NOT NULL,
    LastSync      TEXT
);

CREATE TABLE IF NOT EXISTS BankAccounts(
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    ConnectionId INTEGER NOT NULL,
    UserId       INTEGER NOT NULL,
    AccountName  TEXT NOT NULL,
    Iban         TEXT NOT NULL,
    Currency     TEXT NOT NULL,
    Balance      NUMERIC NOT NULL DEFAULT 0,
    LastSync     TEXT,
    FOREIGN KEY(ConnectionId) REFERENCES BankConnections(Id)
);
"))
                {
                    cmd.ExecuteNonQuery();
                }

                // ---------- Migracje starych baz ----------
                // Users – brakujące kolumny profilu/Email
                if (!Col("Users", "Email")) Cmd("ALTER TABLE Users ADD COLUMN Email           TEXT NULL;").ExecuteNonQuery();
                if (!Col("Users", "FirstName")) Cmd("ALTER TABLE Users ADD COLUMN FirstName       TEXT NULL;").ExecuteNonQuery();
                if (!Col("Users", "LastName")) Cmd("ALTER TABLE Users ADD COLUMN LastName        TEXT NULL;").ExecuteNonQuery();
                if (!Col("Users", "Address")) Cmd("ALTER TABLE Users ADD COLUMN Address         TEXT NULL;").ExecuteNonQuery();
                if (!Col("Users", "CompanyName")) Cmd("ALTER TABLE Users ADD COLUMN CompanyName     TEXT NULL;").ExecuteNonQuery();
                if (!Col("Users", "CompanyNip")) Cmd("ALTER TABLE Users ADD COLUMN CompanyNip      TEXT NULL;").ExecuteNonQuery();
                if (!Col("Users", "CompanyAddress")) Cmd("ALTER TABLE Users ADD COLUMN CompanyAddress  TEXT NULL;").ExecuteNonQuery();

                // Categories – w razie bardzo starej bazy bez UserId
                if (!Col("Categories", "UserId"))
                    Cmd("ALTER TABLE Categories ADD COLUMN UserId INTEGER NULL;").ExecuteNonQuery();

                // ---------- Indeksy ----------
                using (var idx = Cmd(@"
CREATE UNIQUE INDEX IF NOT EXISTS UX_Users_Username_NC
    ON Users(Username COLLATE NOCASE);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Categories_User_Name
    ON Categories(COALESCE(UserId,0), Name);
"))
                {
                    idx.ExecuteNonQuery();
                }

                // ---------- Seed kategorii (gdy pusto) ----------
                using (var check = Cmd("SELECT COUNT(1) FROM Categories;"))
                {
                    var cnt = Convert.ToInt32(check.ExecuteScalar());
                    if (cnt == 0)
                    {
                        using var seed = Cmd(@"
INSERT INTO Categories (Name) VALUES ('Jedzenie');
INSERT INTO Categories (Name) VALUES ('Transport');
INSERT INTO Categories (Name) VALUES ('Rachunki');");
                        seed.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
        }

        private static bool ColumnExists(SqliteConnection con, SqliteTransaction tx, string table, string column)
        {
            using var cmd = con.CreateCommand();
            cmd.Transaction = tx;
            // PRAGMA table_info respektuje transakcję
            cmd.CommandText = $"PRAGMA table_info('{table.Replace("'", "''")}');";

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var name = r["name"]?.ToString();
                if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}






