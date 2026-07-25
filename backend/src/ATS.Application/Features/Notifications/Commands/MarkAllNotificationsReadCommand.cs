using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Notifications.Commands;

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IUnitOfWork _uow;

    public MarkAllNotificationsReadCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Notification>();
        var unread = await repo.Query().Where(n => n.UserId == request.UserId && !n.IsRead).ToListAsync(ct);

        foreach (var n in unread)
        {
            n.IsRead = true;
            repo.Update(n);
        }

        if (unread.Count > 0)
            await _uow.SaveChangesAsync(ct);
    }
}
