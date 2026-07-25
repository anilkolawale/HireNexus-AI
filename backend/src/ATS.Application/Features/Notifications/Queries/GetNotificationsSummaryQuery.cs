using ATS.Application.DTOs.Notifications;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Notifications.Queries;

// Lightweight endpoint for the notification bell: unread count + the last 10, without
// pagination overhead. GetMyNotificationsQuery covers the full paginated history view.
public record GetNotificationsSummaryQuery(Guid UserId) : IRequest<NotificationsSummaryDto>;

public class GetNotificationsSummaryQueryHandler : IRequestHandler<GetNotificationsSummaryQuery, NotificationsSummaryDto>
{
    private readonly IUnitOfWork _uow;

    public GetNotificationsSummaryQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<NotificationsSummaryDto> Handle(GetNotificationsSummaryQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Notification>().Query().Where(n => n.UserId == request.UserId);

        var unreadCount = await query.CountAsync(n => !n.IsRead, ct);
        var recent = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(10)
            .Select(n => new NotificationRowDto(n.Id, n.Title, n.Message, n.IsRead, n.LinkUrl, n.CreatedAtUtc))
            .ToListAsync(ct);

        return new NotificationsSummaryDto(unreadCount, recent);
    }
}
