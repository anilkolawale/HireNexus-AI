using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.Notifications.Commands;

public record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IUnitOfWork _uow;

    public MarkNotificationReadCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Notification>();
        var notification = await repo.GetByIdAsync(request.NotificationId, ct)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        if (notification.UserId != request.UserId)
            throw new ForbiddenAccessException();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            repo.Update(notification);
            await _uow.SaveChangesAsync(ct);
        }
    }
}
