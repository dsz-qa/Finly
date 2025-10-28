using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Finly.Models;
using Finly.Services;

namespace Finly.Pages
{
    public partial class ImportPage : UserControl
    {
        private DataTable? _previewTable;

        public ImportPage()
        {
            InitializeComponent();

            // domyślny separator
            DelimiterBox.SelectedIndex = 0; // ";"

            // podpowiedź domyślnej kategorii – jeśli masz kategorie w DB, możesz je tu zaciągnąć
            try
            {
                var uid = UserService.CurrentUserId;
                if (uid > 0)
                {
                    var cats = DatabaseService.GetCategoriesByUser(uid);
                    if (cats.Count > 0)
                        DefaultCategoryBox.Text = cats.First();
                }
            }
            catch { /* nic – podgląd i tak zadziała */ }
        }

        // ======= UI HANDLERS =======

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Wybierz plik CSV",
                Filter = "CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*",
                CheckFileExists = true
            };
            if (ofd.ShowDialog() == true)
            {
                FilePathBox.Text = ofd.FileName;
                LoadPreview();
            }
        }

        private void ReloadPreview_Click(object sender, RoutedEventArgs e) => LoadPreview();

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            if (_previewTable == null || _previewTable.Rows.Count == 0)
            {
                ToastSafe("Brak danych do importu.", "warning");
                return;
            }

            if (AmountCol.SelectedItem == null || DateCol.SelectedItem == null)
            {
                ToastSafe("Wybierz kolumny: Kwota i Data (wymagane).", "warning");
                return;
            }

            var amountCol = AmountCol.SelectedItem.ToString()!;
            var dateCol = DateCol.SelectedItem.ToString()!;
            var catCol = CatCol.SelectedItem?.ToString();
            var descCol = DescCol.SelectedItem?.ToString();
            var dateFormat = DateFormatBox.Text?.Trim();
            var defaultCategory = DefaultCategoryBox.Text?.Trim();

            var uid = UserService.CurrentUserId;
            if (uid <= 0)
            {
                ToastSafe("Brak zalogowanego użytkownika – import przerwany.", "error");
                return;
            }

            int ok = 0, fail = 0;
            foreach (DataRow row in _previewTable.Rows)
            {
                try
                {
                    // --- odczyty pól ---
                    var amountStr = row[amountCol]?.ToString() ?? "";
                    var dateStr = row[dateCol]?.ToString() ?? "";

                    if (!TryParseAmount(amountStr, out var amount) || amount == 0)
                        throw new InvalidOperationException("Niepoprawna kwota.");

                    if (!TryParseDate(dateStr, dateFormat, out var date))
                        throw new InvalidOperationException("Niepoprawna data.");

                    string? categoryName = null;
                    if (!string.IsNullOrWhiteSpace(catCol))
                        categoryName = row[catCol!]?.ToString();

                    if (string.IsNullOrWhiteSpace(categoryName))
                        categoryName = string.IsNullOrWhiteSpace(defaultCategory) ? "Inne" : defaultCategory;

                    var description = !string.IsNullOrWhiteSpace(descCol) ? (row[descCol!]?.ToString() ?? "") : "";

                    // --- mapowanie kategorii ---
                    var categoryId = DatabaseService.GetOrCreateCategoryId(categoryName!, uid);

                    // --- zapis ---
                    var expense = new Expense
                    {
                        Amount = amount,
                        CategoryId = categoryId,
                        CategoryName = categoryName!,
                        Date = date,
                        Description = description,
                        UserId = uid
                    };

                    DatabaseService.AddExpense(expense);
                    ok++;
                }
                catch
                {
                    fail++;
                }
            }

            if (ok > 0 && fail == 0) ToastSafe($"Zaimportowano {ok} wierszy.", "success");
            else if (ok > 0 && fail > 0) ToastSafe($"Zaimportowano {ok} wierszy, błędy: {fail}.", "warning");
            else ToastSafe("Import nie powiódł się (wszystkie wiersze z błędem).", "error");
        }

        // ======= PREVIEW =======

        private void LoadPreview()
        {
            var path = FilePathBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                ToastSafe("Wybierz poprawny plik.", "warning");
                return;
            }

            char delim = GetDelimiter();
            bool hasHeader = HasHeaderCheckBox.IsChecked == true;

            try
            {
                var table = ReadCsvToDataTable(path, delim, hasHeader, maxRows: 300); // ograniczamy podgląd
                _previewTable = table;

                // Podgląd
                PreviewGrid.ItemsSource = table.DefaultView;

                // Ustaw listy kolumn do mapowania
                var cols = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

                AmountCol.ItemsSource = cols;
                DateCol.ItemsSource = cols;
                CatCol.ItemsSource = cols;
                DescCol.ItemsSource = cols;

                // Proste zgadywanie
                TryAutoSelect(AmountCol, cols, ["kwota", "amount", "value"]);
                TryAutoSelect(DateCol, cols, ["data", "date"]);
                TryAutoSelect(CatCol, cols, ["kategoria", "category", "typ"]);
                TryAutoSelect(DescCol, cols, ["opis", "description", "tytuł", "title"]);
            }
            catch (Exception ex)
            {
                ToastSafe($"Błąd odczytu CSV: {ex.Message}", "error");
            }
        }

        // ======= CSV / PARSING =======

        private static DataTable ReadCsvToDataTable(string path, char delimiter, bool hasHeader, int maxRows = int.MaxValue)
        {
            var table = new DataTable();
            using var sr = new StreamReader(path, DetectEncoding(path));

            // wczytaj pierwszy wiersz -> nagłówki lub liczba kolumn
            var firstLine = sr.ReadLine();
            if (firstLine == null) return table;

            var firstCells = SplitCsvLine(firstLine, delimiter);
            if (hasHeader)
            {
                foreach (var h in firstCells)
                {
                    var name = string.IsNullOrWhiteSpace(h) ? "Kolumna" : h.Trim();
                    table.Columns.Add(MakeUniqueColumnName(table, name));
                }
            }
            else
            {
                for (int i = 0; i < firstCells.Count; i++)
                    table.Columns.Add($"Kolumna{i + 1}");
                // pierwszy wiersz jest danymi
                var row = table.NewRow();
                for (int i = 0; i < firstCells.Count; i++)
                    row[i] = firstCells[i];
                table.Rows.Add(row);
            }

            // reszta wierszy
            string? line;
            int added = hasHeader ? 0 : 1;
            while (added < maxRows && (line = sr.ReadLine()) != null)
            {
                var cells = SplitCsvLine(line, delimiter);
                // dopasuj liczbę kolumn
                while (cells.Count < table.Columns.Count) cells.Add(string.Empty);
                while (cells.Count > table.Columns.Count) cells.RemoveAt(cells.Count - 1);

                var row = table.NewRow();
                for (int i = 0; i < table.Columns.Count; i++)
                    row[i] = cells[i];
                table.Rows.Add(row);
                added++;
            }

            return table;
        }

        private static Encoding DetectEncoding(string path)
        {
            // prosty heurystyczny wybór – UTF8 bez BOM zwykle ok
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        private static List<string> SplitCsvLine(string line, char delimiter)
        {
            // prosty parser CSV z obsługą cudzysłowów
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        // podwojony cudzysłów => znak "
                        sb.Append('\"'); i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == delimiter && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(ch);
                }
            }
            result.Add(sb.ToString());
            return result;
        }

        private static string MakeUniqueColumnName(DataTable table, string baseName)
        {
            string name = baseName;
            int i = 1;
            while (table.Columns.Contains(name))
            {
                i++;
                name = $"{baseName}_{i}";
            }
            return name;
        }

        private char GetDelimiter()
        {
            var sel = (DelimiterBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return sel switch
            {
                "Tab" => '\t',
                "," => ',',
                _ => ';'
            };
        }

        private static bool TryParseAmount(string input, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // akceptuj formaty z kropką i przecinkiem
            var s = input.Trim();

            // usuń spacje tysięczne
            s = s.Replace(" ", "").Replace("\u00A0", "");

            // zamień przecinek na kropkę (najczęstszy przypadek CSV PL)
            if (s.Contains(',') && !s.Contains('.'))
                s = s.Replace(',', '.');

            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseDate(string input, string? format, out DateTime date)
        {
            date = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(input)) return false;

            var s = input.Trim();

            if (!string.IsNullOrWhiteSpace(format))
            {
                if (DateTime.TryParseExact(s, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out date)) return true;

                // Spróbuj też PL
                if (DateTime.TryParseExact(s, format, new CultureInfo("pl-PL"),
                    DateTimeStyles.None, out date)) return true;
            }

            // Fallback – spróbuj dowolne znane formaty
            if (DateTime.TryParse(s, new CultureInfo("pl-PL"), DateTimeStyles.None, out date)) return true;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) return true;

            return false;
        }

        private static void TryAutoSelect(ComboBox box, List<string> cols, IEnumerable<string> hints)
        {
            var idx = -1;
            var lowered = cols.Select(c => c.Trim().ToLowerInvariant()).ToList();
            foreach (var h in hints)
            {
                var i = lowered.FindIndex(c => c.Contains(h));
                if (i >= 0) { idx = i; break; }
            }
            if (idx >= 0) box.SelectedItem = cols[idx];
        }

        private static void ToastSafe(string msg, string type)
        {
            try
            {
                switch (type)
                {
                    case "success": ToastService.Success(msg); break;
                    case "warning": ToastService.Warning(msg); break;
                    case "error": ToastService.Error(msg); break;
                    default: ToastService.Info(msg); break;
                }
            }
            catch
            {
                MessageBox.Show(msg);
            }
        }
    }
}
