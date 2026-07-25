namespace ATS.Application.DTOs.Candidates;

public record TalentPoolRowDto(
    Guid CandidateId,
    string FullName,
    string Email,
    string? Headline,
    string? CurrentEmployer,
    IReadOnlyList<string> Skills,
    int TotalApplications,
    int? BestMatchScore,
    DateTime ProfileCreatedAtUtc);
