namespace ATS.Application.DTOs.Sessions;

public record SessionDto(
    Guid Id,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime LastUsedAtUtc,
    DateTime ExpiresAtUtc,
    bool IsCurrent);
