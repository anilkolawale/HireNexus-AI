namespace ATS.Application.DTOs.Candidates;

public record CandidateProfileDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? Headline,
    string? Summary,
    string? CurrentEmployer,
    decimal? ExpectedSalary,
    string? LinkedInUrl,
    string? PortfolioUrl,
    string? ResumeUrl,
    IReadOnlyList<string> Skills,
    IReadOnlyList<EducationDto> Education,
    IReadOnlyList<ExperienceDto> Experience,
    IReadOnlyList<string> Certifications);

public record EducationDto(Guid Id, string Institution, string Degree, string? FieldOfStudy, int? StartYear, int? EndYear);

public record ExperienceDto(Guid Id, string CompanyName, string Title, DateTime StartDate, DateTime? EndDate, string? Description);

public record UpdateCandidateProfileDto(
    string? Headline,
    string? Summary,
    string? CurrentEmployer,
    decimal? ExpectedSalary,
    string? LinkedInUrl,
    string? PortfolioUrl);

public record ResumeUploadResultDto(
    string ResumeUrl,
    IReadOnlyList<string> ExtractedSkills,
    IReadOnlyList<string> MissingFields,
    string AiSummary);
