using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.Webhooks.Commands;

public record DeleteWebhookSubscriptionCommand(Guid Id, Guid RequestingCompanyId, bool IsSuperAdmin) : IRequest;

public class DeleteWebhookSubscriptionCommandHandler : IRequestHandler<DeleteWebhookSubscriptionCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteWebhookSubscriptionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteWebhookSubscriptionCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<WebhookSubscription>();
        var subscription = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(WebhookSubscription), request.Id);

        if (!request.IsSuperAdmin && subscription.CompanyId != request.RequestingCompanyId)
            throw new ForbiddenAccessException();

        repo.Remove(subscription);
        await _uow.SaveChangesAsync(ct);
    }
}
