namespace ATS.Application.DTOs.Candidates;

public record ResumeHistoryRowDto(Guid Id, string FileName, int Version, string BlobUrl, DateTime UploadedAtUtc, bool IsCurrent);
