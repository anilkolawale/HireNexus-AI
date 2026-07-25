namespace ATS.Application.Common.Interfaces;

public interface IReportExportService
{
    byte[] ExportToExcel<T>(string title, IReadOnlyList<T> rows);
    byte[] ExportToPdf<T>(string title, IReadOnlyList<T> rows);
}
