using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;

using Finly.Models;
using Finly.Services;

// QuestPDF
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Finly.Views
{
    public partial class ChartView : Window
    {
        private readonly int _userId;
        private List<Expense> expenses = new();

        public ChartView(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadChartData(); // wczytaj wszystkie dane na start
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

            expenses = filtered
                .Select(e => new Expense
                {
                    Id = e.Id,
                    Amount = e.Amount,
                    CategoryId = e.CategoryId,
                    Category = e.CategoryName ?? "",
                    UserId = e.UserId,
                    Date = e.Date,
                    Description = e.Description ?? string.Empty
                }).ToList();

            var display = expenses.Select(x => new ExpenseDisplayModel
            {
                Amount = x.Amount,
                CategoryName = x.Category,
                Date = x.Date,
                Description = x.Description
            }).ToList();

            LoadPieChart(display);
            LoadLineChart(display);
        }

        private void LoadPieChart(List<ExpenseDisplayModel> data)
        {
            var grouped = data
                .GroupBy(e => e.CategoryName)
                .Select(g => new PieSeries<decimal>
                {
                    Name = g.Key ?? "Brak kategorii",
                    Values = new List<decimal> { (decimal)g.Sum(e => e.Amount) },
                    DataLabelsSize = 14,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.PrimaryValue:N0} z³"
                })
                .ToArray();

            pieChart.Series = grouped;
        }

        private void LoadLineChart(List<ExpenseDisplayModel> data)
        {
            var grouped = data
                .GroupBy(e => e.Date.Date)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    Date = g.Key,
                    Sum = g.Sum(x => x.Amount)
                })
                .ToList();

            lineChart.Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = grouped.Select(g => g.Sum).ToArray(),
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(new SKColor(30,144,255), 3),
                    Fill = null
                }
            };

            lineChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = grouped.Select(g => g.Date.ToString("dd.MM")).ToArray(),
                    LabelsRotation = 0,
                    TextSize = 14
                }
            };

            lineChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Labeler = value => value.ToString("N0") + " z³",
                    TextSize = 14
                }
            };
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (SortComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (expenses == null || expenses.Count == 0) return;

            IEnumerable<Expense> sorted = expenses;

            switch (selected)
            {
                case "Kwota rosn¹co": sorted = expenses.OrderBy(x => x.Amount); break;
                case "Kwota malej¹co": sorted = expenses.OrderByDescending(x => x.Amount); break;
                case "Data rosn¹co": sorted = expenses.OrderBy(x => x.Date); break;
                case "Data malej¹co": sorted = expenses.OrderByDescending(x => x.Date); break;
            }

            var display = sorted.Select(x => new ExpenseDisplayModel
            {
                Amount = x.Amount,
                Date = x.Date,
                CategoryName = x.Category,
                Description = x.Description
            }).ToList();

            LoadLineChart(display);
        }

        // ====== Eksport do PNG + PDF (QuestPDF) ======
        private void ExportChartsToPdf()
        {
            string pieChartPath = Path.Combine(Path.GetTempPath(), $"finly_pie_{Guid.NewGuid():N}.png");
            string lineChartPath = Path.Combine(Path.GetTempPath(), $"finly_line_{Guid.NewGuid():N}.png");

            SaveElementAsPng(pieChart, pieChartPath);
            SaveElementAsPng(lineChart, lineChartPath);

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF File|*.pdf",
                FileName = "Wykresy"
            };
            if (saveDialog.ShowDialog() != true) return;

            var pieBytes = File.ReadAllBytes(pieChartPath);
            var lineBytes = File.ReadAllBytes(lineChartPath);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.Content()
                        .Column(col =>
                        {
                            col.Spacing(15);
                            col.Item().Image(pieBytes);
                            col.Item().Image(lineBytes);
                        });
                });
            })
            .GeneratePdf(saveDialog.FileName);

            try { File.Delete(pieChartPath); } catch { }
            try { File.Delete(lineChartPath); } catch { }
        }

        private static void SaveElementAsPng(FrameworkElement element, string path)
        {
            var rtb = new RenderTargetBitmap(
                Math.Max(1, (int)element.ActualWidth),
                Math.Max(1, (int)element.ActualHeight),
                96, 96, PixelFormats.Pbgra32);

            rtb.Render(element);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var fs = File.Create(path);
            encoder.Save(fs);
        }
    }
}
