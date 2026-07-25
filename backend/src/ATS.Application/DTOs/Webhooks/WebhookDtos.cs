namespace ATS.Application.DTOs.Webhooks;

public record WebhookSubscriptionDto(
    Guid Id,
    string Url,
    IReadOnlyList<string> EventTypes,
    bool IsActive,
    DateTime CreatedAtUtc);

// Secret is only ever returned once, at creation time — never again on subsequent reads,
// same as how API keys are typically handled (Stripe, GitHub, etc.) so it can't leak via
// a list endpoint later.
public record CreatedWebhookSubscriptionDto(
    Guid Id,
    string Url,
    string Secret,
    IReadOnlyList<string> EventTypes);

public record WebhookDeliveryLogDto(
    Guid Id,
    string EventType,
    int? ResponseStatusCode,
    bool Success,
    string? ErrorMessage,
    DateTime AttemptedAtUtc);

public static class WebhookEventTypes
{
    public static readonly string[] All =
    {
        "job.published",
        "job.closed",
        "application.status_changed",
        "candidate.hired",
        "offer.extended"
    };
}
