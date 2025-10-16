using Finly.Models;
using Finly.Services;   // Twoje serwisy DB
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Finly.Services
{
    public static class ReportService
    {
        // Snapshocik kontrolki (np. wykresu) -> PNG bytes
        public static byte[] SnapshotToPng(FrameworkElement element, double dpi = 192)
        {
            var width = (int)Math.Max(1, element.ActualWidth);
            var height = (int)Math.Max(1, element.ActualHeight);
            var rtb = new RenderTargetBitmap(width, height, dpi, dpi, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(element);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var ms = new MemoryStream();
            encoder.Save(ms);
            return ms.ToArray();
        }

        public static void GenerateMonthlyReport(int userId, DateTime month, string outputPath,
                                                 byte[]? chartPng = null)
        {
            // 1) Dane (tu użyj swojego DatabaseService / repo)
            var (from, to) = (new DateTime(month.Year, month.Month, 1),
                              new DateTime(month.Year, month.Month, 1).AddMonths(1).AddTicks(-1));

            var expenses = DatabaseService.GetExpenses(userId, from, to); // Załóżmy, że masz taką metodę
            var total = expenses.Sum(x => x.Amount);
            var byCategory = expenses
                .GroupBy(x => x.CategoryName)
                .Select(g => new { Category = g.Key, Sum = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Sum)
                .ToList();

            // 2) PDF
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(28);
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Raport miesięczny").SemiBold().FontSize(18);
                            col.Item().Text($"{from:MMMM yyyy}").FontSize(12).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(80).Height(40).Placeholder(); // logo opcjonalnie
                    });

                    page.Content().Column(col =>
                    {
                        // KPI
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Text($"Suma wydatków: {total:C}");
                            r.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Text($"Transakcji: {expenses.Count}");
                        });

                        // Wykres jako obraz (opcjonalnie)
                        if (chartPng is not null)
                            col.Item().PaddingVertical(10).Image(chartPng);

                        // Tabela kategorii
                        col.Item().PaddingTop(10).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                            });
                            t.Header(h =>
                            {
                                h.Cell().Element(CellHeader).Text("Kategoria");
                                h.Cell().Element(CellHeader).AlignRight().Text("Suma");
                                static IContainer CellHeader(IContainer c) =>
                                    c.Background(Colors.Grey.Lighten3).Padding(6).DefaultTextStyle(x => x.SemiBold());
                            });

                            foreach (var row in byCategory)
                            {
                                t.Cell().Padding(6).Text(row.Category);
                                t.Cell().Padding(6).AlignRight().Text($"{row.Sum:C}");
                            }
                        });

                        // Tabela transakcji
                        col.Item().PaddingTop(10).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Data
                                c.RelativeColumn(6); // Opis
                                c.RelativeColumn(2); // Kwota
                            });
                            t.Header(h =>
                            {
                                CellHeader(h.Cell()).Text("Data");
                                CellHeader(h.Cell()).Text("Opis");
                                CellHeader(h.Cell()).AlignRight().Text("Kwota");
                                static IContainer CellHeader(IContainer c) =>
                                    c.Background(Colors.Grey.Lighten3).Padding(6).DefaultTextStyle(x => x.SemiBold());
                            });

                            foreach (var e in expenses)
                            {
                                t.Cell().Padding(6).Text(e.Date.ToString("yyyy-MM-dd"));
                                t.Cell().Padding(6).Text(e.Description);
                                t.Cell().Padding(6).AlignRight().Text($"{e.Amount:C}");
                            }
                        });
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Strona ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(outputPath);
        }
    }
}
