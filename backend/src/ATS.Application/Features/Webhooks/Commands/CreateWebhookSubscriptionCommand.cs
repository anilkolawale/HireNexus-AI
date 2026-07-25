using System.Security.Cryptography;
using ATS.Application.DTOs.Webhooks;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.Webhooks.Commands;

public record CreateWebhookSubscriptionCommand(Guid CompanyId, string Url, List<string> EventTypes)
    : IRequest<CreatedWebhookSubscriptionDto>;

public class CreateWebhookSubscriptionCommandValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    public CreateWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Url).NotEmpty().Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == "https")
            .WithMessage("Webhook URL must be a valid HTTPS URL.");
        RuleFor(x => x.EventTypes).NotEmpty()
            .Must(types => types.All(WebhookEventTypes.All.Contains))
            .WithMessage($"Event types must be one of: {string.Join(", ", WebhookEventTypes.All)}");
    }
}

public class CreateWebhookSubscriptionCommandHandler : IRequestHandler<CreateWebhookSubscriptionCommand, CreatedWebhookSubscriptionDto>
{
    private readonly IUnitOfWork _uow;

    public CreateWebhookSubscriptionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<CreatedWebhookSubscriptionDto> Handle(CreateWebhookSubscriptionCommand request, CancellationToken ct)
    {
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        var subscription = new WebhookSubscription
        {
            CompanyId = request.CompanyId,
            Url = request.Url,
            Secret = secret,
            EventTypesCsv = string.Join(",", request.EventTypes),
            IsActive = true
        };

        await _uow.Repository<WebhookSubscription>().AddAsync(subscription, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreatedWebhookSubscriptionDto(subscription.Id, subscription.Url, secret, request.EventTypes);
    }
}
