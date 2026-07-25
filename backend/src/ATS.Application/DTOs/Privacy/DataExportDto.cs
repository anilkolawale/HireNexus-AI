namespace ATS.Application.DTOs.Privacy;

// GDPR Article 20 (data portability): a full, human-readable export of everything the
// platform holds about a candidate, in one JSON document they can download and take elsewhere.
public record DataExportDto(
    DataExportProfile Profile,
    IReadOnlyList<DataExportApplication> Applications,
    IReadOnlyList<DataExportResumeVersion> ResumeHistory,
    DateTime ExportedAtUtc);

public record DataExportProfile(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Headline,
    string? Summary,
    string? CurrentEmployer,
    decimal? ExpectedSalary,
    string? LinkedInUrl,
    string? PortfolioUrl,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Education,
    IReadOnlyList<string> Experience,
    IReadOnlyList<string> Certifications,
    DateTime AccountCreatedAtUtc);

public record DataExportApplication(
    string JobTitle,
    string CompanyName,
    string Status,
    int? MatchScore,
    DateTime AppliedAtUtc);

public record DataExportResumeVersion(string FileName, int Version, DateTime UploadedAtUtc);
