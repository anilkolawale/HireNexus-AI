using ATS.Application.DTOs.Webhooks;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Webhooks.Queries;

public record GetWebhookSubscriptionsQuery(Guid CompanyId) : IRequest<IReadOnlyList<WebhookSubscriptionDto>>;

public class GetWebhookSubscriptionsQueryHandler : IRequestHandler<GetWebhookSubscriptionsQuery, IReadOnlyList<WebhookSubscriptionDto>>
{
    private readonly IUnitOfWork _uow;

    public GetWebhookSubscriptionsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<WebhookSubscriptionDto>> Handle(GetWebhookSubscriptionsQuery request, CancellationToken ct)
    {
        var subscriptions = await _uow.Repository<WebhookSubscription>().Query()
            .Where(w => w.CompanyId == request.CompanyId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(ct);

        return subscriptions.Select(w => new WebhookSubscriptionDto(
            w.Id, w.Url, w.EventTypesCsv.Split(',', StringSplitOptions.TrimEntries), w.IsActive, w.CreatedAtUtc)).ToList();
    }
}
