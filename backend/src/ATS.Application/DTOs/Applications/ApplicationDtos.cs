using ATS.Domain.Enums;

namespace ATS.Application.DTOs.Applications;

public record ApplicationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    Guid CandidateId,
    string CandidateName,
    ApplicationStatus Status,
    int? MatchScore,
    DateTime CreatedAtUtc);

public record ApplicationDetailDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string? CandidateName,
    ApplicationStatus Status,
    int? MatchScore,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> RecommendedSkills,
    string? AiRecommendation,
    DateTime CreatedAtUtc);


public record ChangeApplicationStatusDto(ApplicationStatus NewStatus, string? Notes);
