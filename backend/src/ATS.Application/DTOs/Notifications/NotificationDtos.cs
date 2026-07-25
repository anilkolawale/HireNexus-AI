namespace ATS.Application.DTOs.Notifications;

public record NotificationRowDto(
    Guid Id,
    string Title,
    string Message,
    bool IsRead,
    string? LinkUrl,
    DateTime CreatedAtUtc);

public record NotificationsSummaryDto(int UnreadCount, IReadOnlyList<NotificationRowDto> Recent);
