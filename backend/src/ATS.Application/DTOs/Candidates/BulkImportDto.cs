namespace ATS.Application.DTOs.Candidates;

public record BulkImportRowResultDto(int RowNumber, string Email, bool Success, string? Error);
public record BulkImportResultDto(int TotalRows, int Succeeded, int Failed, IReadOnlyList<BulkImportRowResultDto> Rows);
