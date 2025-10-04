using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Aplikacja_do_sledzenia_wydatkow.Models;
using Aplikacja_do_sledzenia_wydatkow.Services;
using Aplikacja_do_sledzenia_wydatkow.Models;
using Aplikacja_do_sledzenia_wydatkow.Services;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using PdfSharp.Drawing;

namespace Aplikacja_do_sledzenia_wydatkow.Views
{
    public partial class ChartView : Window
    {
        private readonly int _userId;

        public ChartView(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadChartData(); // Wczytaj wszystkie dane na start
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var start = FromDatePicker.SelectedDate;
            var end = ToDatePicker.SelectedDate;
            LoadChartData(start, end);
        }

        private void LoadChartData(DateTime? start = null, DateTime? end = null)
        {
            var allExpenses = DatabaseService.GetExpensesWithCategory()
            .Where(e => e.UserId == _userId)
            .ToList();

            var filtered = allExpenses
                .Where(e =>
                    (!start.HasValue || e.Date >= start.Value) &&
                    (!end.HasValue || e.Date <= end.Value))
                .ToList();

            LoadPieChart(filtered);
            LoadLineChart(filtered);

            expenses = filtered.Select(e => new Expense
            {
                Id = e.Id,
                Amount = e.Amount,
                Category = e.CategoryName,
                Date = e.Date
            }).ToList();
        }

        private void LoadPieChart(List<ExpenseDisplayModel> expenses)
        {
            var grouped = expenses
                .GroupBy(e => e.CategoryName)
                .Select(g => new PieSeries<decimal>
                {
                    Name = g.Key,
                    Values = new List<decimal> { (decimal)g.Sum(e => e.Amount) },
                    DataLabelsSize = 14,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.PrimaryValue:N0} z³"
                })
                .ToArray();

            pieChart.Series = grouped;
        }

        private void LoadLineChart(List<ExpenseDisplayModel> expenses)
        {
            var grouped = expenses
                .GroupBy(e => e.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(e => e.Amount)
                })
                .ToList();

            lineChart.Series = new ISeries[]
            {
                new LineSeries<decimal>
                {
                    Values = grouped.Select(g => (decimal)g.Amount).ToArray(),
                    Fill = null,
                    GeometrySize = 10
                }
            };

            lineChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = grouped.Select(g => g.Date.ToString("dd.MM.yyyy")).ToArray(),
                    LabelsRotation = 45,
                    Name = "Data"
                }
            };

            lineChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Kwota [z³]",
                    LabelsPaint = new SolidColorPaint(SKColors.Black),
                    Labeler = value => $"{value / 1000:N1} tys."
                }
            };
        }

        private void ExportChartsToPng_Click(object sender, RoutedEventArgs e)
        {
            var saveDialogPie = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = "WykresKolowy"
            };

            if (saveDialogPie.ShowDialog() != true) return;

            var saveDialogLine = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = "WykresLiniowy"
            };

            if (saveDialogLine.ShowDialog() != true) return;

            // Zapis PieChart
            var pieBitmap = new RenderTargetBitmap((int)pieChart.ActualWidth, (int)pieChart.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            pieBitmap.Render(pieChart);
            var pieEncoder = new PngBitmapEncoder();
            pieEncoder.Frames.Add(BitmapFrame.Create(pieBitmap));
            using (var stream = File.Create(saveDialogPie.FileName))
            {
                pieEncoder.Save(stream);
            }

            // Zapis LineChart
            var lineBitmap = new RenderTargetBitmap((int)lineChart.ActualWidth, (int)lineChart.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            lineBitmap.Render(lineChart);
            var lineEncoder = new PngBitmapEncoder();
            lineEncoder.Frames.Add(BitmapFrame.Create(lineBitmap));
            using (var stream = File.Create(saveDialogLine.FileName))
            {
                lineEncoder.Save(stream);
            }

            MessageBox.Show("Wykresy zapisano jako pliki PNG.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportChartToPdf_Click(object sender, RoutedEventArgs e)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            string pieChartPath = System.IO.Path.GetTempFileName() + "_pie.png";
            string lineChartPath = System.IO.Path.GetTempFileName() + "_line.png";

            // Render PieChart
            var pieBitmap = new RenderTargetBitmap((int)pieChart.ActualWidth, (int)pieChart.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            pieBitmap.Render(pieChart);
            var pieEncoder = new PngBitmapEncoder();
            pieEncoder.Frames.Add(BitmapFrame.Create(pieBitmap));
            using (var stream = File.Create(pieChartPath)) pieEncoder.Save(stream);

            // Render LineChart
            var lineBitmap = new RenderTargetBitmap((int)lineChart.ActualWidth, (int)lineChart.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            lineBitmap.Render(lineChart);
            var lineEncoder = new PngBitmapEncoder();
            lineEncoder.Frames.Add(BitmapFrame.Create(lineBitmap));
            using (var stream = File.Create(lineChartPath)) lineEncoder.Save(stream);

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Document|*.pdf",
                FileName = "wykresy_wydatków"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var doc = new PdfSharp.Pdf.PdfDocument();
                var page = doc.AddPage();

                using (var gfx = XGraphics.FromPdfPage(page))
                using (var imgPie = XImage.FromFile(pieChartPath))
                using (var imgLine = XImage.FromFile(lineChartPath))
                {
                    double margin = 20;
                    double availableWidth = page.Width - 2 * margin;
                    double halfHeight = (page.Height - 3 * margin) / 2;

                    // Rysowanie PieChart
                    gfx.DrawImage(imgPie, margin, margin, availableWidth, halfHeight);

                    // Rysowanie LineChart pod spodem
                    gfx.DrawImage(imgLine, margin, margin + halfHeight + margin, availableWidth, halfHeight);
                }

                doc.Save(saveDialog.FileName);
                MessageBox.Show("Wykresy zosta³y zapisane jako PDF.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            File.Delete(pieChartPath);
            File.Delete(lineChartPath);
        }

        private List<Expense> expenses; // Lista wydatków

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selected = (SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (expenses == null)
            {
                MessageBox.Show("Brak danych do sortowania.", "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<Expense> sortedExpenses = new List<Expense>(expenses);

            if (selected == "Kategoria A-Z")
                sortedExpenses = expenses.OrderBy(x => x.Category).ToList();
            else if (selected == "Kategoria Z-A")
                sortedExpenses = expenses.OrderByDescending(x => x.Category).ToList();
            else if (selected == "Suma rosn¹co")
                sortedExpenses = expenses.OrderBy(x => x.Amount).ToList();
            else if (selected == "Suma malej¹co")
                sortedExpenses = expenses.OrderByDescending(x => x.Amount).ToList();

            // Odœwie¿ wykres po sortowaniu
            var displayExpenses = sortedExpenses.Select(x => new ExpenseDisplayModel
            {
                Id = x.Id,
                Amount = x.Amount,
                CategoryName = x.Category,
                Date = x.Date
            }).ToList();

            // Odœwie¿ wykres
            LoadPieChart(displayExpenses);
        }

        private void DateSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selected = (DateSortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            List<Expense> sortedExpenses = new List<Expense>(expenses);

            if (selected == "Data rosn¹co")
                sortedExpenses = expenses.OrderBy(x => x.Date).ToList();
            else if (selected == "Data malej¹co")
                sortedExpenses = expenses.OrderByDescending(x => x.Date).ToList();

            var convertedExpenses = sortedExpenses.Select(x => new ExpenseDisplayModel
            {
                Amount = x.Amount,
                Date = x.Date,
                CategoryName = x.Category,
                Description = x.Description
            }).ToList();

            LoadLineChart(convertedExpenses);
        }
    }
}