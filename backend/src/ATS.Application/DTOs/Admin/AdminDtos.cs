namespace ATS.Application.DTOs.Admin;

public record AdminDashboardDto(
    int TotalUsers,
    int TotalCompanies,
    int TotalJobs,
    int TotalApplications,
    IReadOnlyList<RoleCountDto> UsersByRole);

public record RoleCountDto(string Role, int Count);

public record AuditLogRowDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    string Action,
    string EntityName,
    Guid? EntityId,
    DateTime TimestampUtc);

public record UserManagementRowDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    string? CompanyName,
    bool IsActive,
    bool IsEmailVerified,
    DateTime CreatedAtUtc);
