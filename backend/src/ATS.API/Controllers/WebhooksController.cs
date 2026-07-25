using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Webhooks;
using ATS.Application.Features.Webhooks.Commands;
using ATS.Application.Features.Webhooks.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

// Generic outbound-webhook system for job-board syndication and general integrations.
// Deliberately provider-agnostic rather than baking in LinkedIn/Indeed-specific OAuth flows —
// those require live developer accounts and API credentials this environment doesn't have.
// A company points their own integration (Zapier, a custom listener, or eventually a real
// LinkedIn/Indeed connector) at their webhook URL and receives HMAC-signed events.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public WebhooksController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    private bool IsSuperAdmin => _currentUser.Role == "SuperAdmin";
    private Guid CompanyIdOrEmpty => _currentUser.CompanyId ?? Guid.Empty;

    /// <summary>Available event types a webhook subscription can listen for.</summary>
    [HttpGet("event-types")]
    public IActionResult GetEventTypes() => Ok(WebhookEventTypes.All);

    /// <summary>List this company's webhook subscriptions. Secrets are never included here — only at creation.</summary>
    [HttpGet]
    public async Task<IActionResult> GetSubscriptions(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWebhookSubscriptionsQuery(CompanyIdOrEmpty), ct);
        return Ok(result);
    }

    public record CreateWebhookRequest(string Url, List<string> EventTypes);

    /// <summary>
    /// Register a new webhook subscription. Returns the signing secret exactly once — store it,
    /// it cannot be retrieved again (same pattern as Stripe/GitHub API keys).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSubscription(CreateWebhookRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateWebhookSubscriptionCommand(CompanyIdOrEmpty, body.Url, body.EventTypes), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteWebhookSubscriptionCommand(id, CompanyIdOrEmpty, IsSuperAdmin), ct);
        return NoContent();
    }

    /// <summary>Recent delivery attempts for a subscription — status codes, errors — for debugging integrations.</summary>
    [HttpGet("{id:guid}/deliveries")]
    public async Task<IActionResult> GetDeliveries(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWebhookDeliveryLogsQuery(id, CompanyIdOrEmpty, IsSuperAdmin), ct);
        return Ok(result);
    }
}
