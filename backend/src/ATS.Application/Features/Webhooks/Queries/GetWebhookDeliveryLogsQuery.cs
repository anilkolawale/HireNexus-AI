using ATS.Application.DTOs.Webhooks;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Webhooks.Queries;

public record GetWebhookDeliveryLogsQuery(Guid SubscriptionId, Guid RequestingCompanyId, bool IsSuperAdmin)
    : IRequest<IReadOnlyList<WebhookDeliveryLogDto>>;

public class GetWebhookDeliveryLogsQueryHandler : IRequestHandler<GetWebhookDeliveryLogsQuery, IReadOnlyList<WebhookDeliveryLogDto>>
{
    private readonly IUnitOfWork _uow;

    public GetWebhookDeliveryLogsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<WebhookDeliveryLogDto>> Handle(GetWebhookDeliveryLogsQuery request, CancellationToken ct)
    {
        var subscription = await _uow.Repository<WebhookSubscription>().GetByIdAsync(request.SubscriptionId, ct)
            ?? throw new NotFoundException(nameof(WebhookSubscription), request.SubscriptionId);

        if (!request.IsSuperAdmin && subscription.CompanyId != request.RequestingCompanyId)
            throw new ForbiddenAccessException();

        var logs = await _uow.Repository<WebhookDeliveryLog>().Query()
            .Where(l => l.WebhookSubscriptionId == request.SubscriptionId)
            .OrderByDescending(l => l.AttemptedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        return logs.Select(l => new WebhookDeliveryLogDto(
            l.Id, l.EventType, l.ResponseStatusCode, l.Success, l.ErrorMessage, l.AttemptedAtUtc)).ToList();
    }
}
