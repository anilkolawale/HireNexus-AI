using ATS.Domain.Common;

namespace ATS.Domain.Entities;

public class WebhookSubscription : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public string Url { get; set; } = default!;
    public string Secret { get; set; } = default!; // used to HMAC-sign delivered payloads
    public string EventTypesCsv { get; set; } = default!; // e.g. "job.published,application.status_changed"
    public bool IsActive { get; set; } = true;

    public ICollection<WebhookDeliveryLog> Deliveries { get; set; } = new List<WebhookDeliveryLog>();
}

public class WebhookDeliveryLog : BaseEntity
{
    public Guid WebhookSubscriptionId { get; set; }
    public WebhookSubscription WebhookSubscription { get; set; } = default!;

    public string EventType { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public int? ResponseStatusCode { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
}
