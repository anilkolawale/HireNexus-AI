using System.Reflection;
using ATS.Application.Common.Interfaces;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATS.Infrastructure.Reports;

// Generic exporter: uses reflection over the record's public properties so every report
// type (HiringReportRow, RecruiterPerformanceRow, etc.) gets Excel/PDF export for free —
// no per-report export code needed.
public class ReportExportService : IReportExportService
{
    static ReportExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] ExportToExcel<T>(string title, IReadOnlyList<T> rows)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(Truncate(title, 31));

        for (var col = 0; col < properties.Length; col++)
            sheet.Cell(1, col + 1).Value = SplitPascalCase(properties[col].Name);

        sheet.Row(1).Style.Font.Bold = true;

        for (var row = 0; row < rows.Count; row++)
        {
            for (var col = 0; col < properties.Length; col++)
            {
                var value = properties[col].GetValue(rows[row]);
                sheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] ExportToPdf<T>(string title, IReadOnlyList<T> rows)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Text(title).FontSize(16).Bold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in properties) columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var prop in properties)
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(SplitPascalCase(prop.Name)).Bold();
                    });

                    foreach (var row in rows)
                    {
                        foreach (var prop in properties)
                        {
                            var value = prop.GetValue(row);
                            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value?.ToString() ?? "");
                        }
                    }
                });

                page.Footer().AlignCenter().Text($"Generated {DateTime.UtcNow:g} UTC · {rows.Count} rows");
            });
        });

        return document.GeneratePdf();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private static string SplitPascalCase(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
}
