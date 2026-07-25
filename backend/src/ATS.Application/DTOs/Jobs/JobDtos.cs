using ATS.Domain.Enums;

namespace ATS.Application.DTOs.Jobs;

public record JobDto(
    Guid Id,
    string Title,
    string Description,
    string Department,
    string ExperienceRequired,
    decimal SalaryMin,
    decimal SalaryMax,
    string Location,
    EmploymentType EmploymentType,
    JobStatus Status,
    IReadOnlyList<string> Skills,
    DateTime CreatedAtUtc);

public record JobListItemDto(
    Guid Id,
    string Title,
    string Department,
    string Location,
    EmploymentType EmploymentType,
    JobStatus Status,
    int ApplicationCount,
    DateTime CreatedAtUtc);
