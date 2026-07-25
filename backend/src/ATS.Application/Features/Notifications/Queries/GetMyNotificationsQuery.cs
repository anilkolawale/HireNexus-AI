using ATS.Application.Common.Models;
using ATS.Application.DTOs.Notifications;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Notifications.Queries;

public record GetMyNotificationsQuery(Guid UserId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<NotificationRowDto>>;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, PaginatedList<NotificationRowDto>>
{
    private readonly IUnitOfWork _uow;

    public GetMyNotificationsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedList<NotificationRowDto>> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Notification>().Query()
            .Where(n => n.UserId == request.UserId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new NotificationRowDto(n.Id, n.Title, n.Message, n.IsRead, n.LinkUrl, n.CreatedAtUtc));

        return await PaginatedList<NotificationRowDto>.CreateAsync(query, request.PageNumber, request.PageSize);
    }
}
