using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ATS.Infrastructure.Notifications;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IUnitOfWork _uow;

    public SignalRNotificationService(IHubContext<NotificationHub> hub, IUnitOfWork uow)
    {
        _hub = hub;
        _uow = uow;
    }

    public async Task NotifyUserAsync(Guid userId, string title, string message, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = NotificationType.General,
            Title = title,
            Message = message
        };
        await _uow.Repository<Notification>().AddAsync(notification, ct);
        await _uow.SaveChangesAsync(ct);

        await _hub.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", new
        {
            notification.Id,
            notification.Title,
            notification.Message,
            notification.CreatedAtUtc
        }, ct);
    }
}
