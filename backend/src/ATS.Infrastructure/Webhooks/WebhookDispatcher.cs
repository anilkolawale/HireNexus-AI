using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Webhooks;

// Fire-and-forget-ish: delivery failures are logged, not thrown, since a slow or dead
// subscriber endpoint should never block or fail the primary operation (e.g. publishing a
// job) that triggered the webhook. Not a full retry/backoff queue — see README for that gap.
public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IUnitOfWork _uow;
    private readonly HttpClient _http;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(IUnitOfWork uow, HttpClient http, ILogger<WebhookDispatcher> logger)
    {
        _uow = uow;
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;
    }

    public async Task DispatchAsync(Guid companyId, string eventType, object payload, CancellationToken ct = default)
    {
        var subscriptions = await _uow.Repository<WebhookSubscription>().Query()
            .Where(w => w.CompanyId == companyId && w.IsActive)
            .ToListAsync(ct);

        var relevant = subscriptions.Where(w =>
            w.EventTypesCsv.Split(',', StringSplitOptions.TrimEntries).Contains(eventType)).ToList();

        if (relevant.Count == 0) return;

        var payloadJson = JsonSerializer.Serialize(new { eventType, occurredAtUtc = DateTime.UtcNow, data = payload });

        foreach (var subscription in relevant)
        {
            await DeliverAsync(subscription, eventType, payloadJson, ct);
        }
    }

    private async Task DeliverAsync(WebhookSubscription subscription, string eventType, string payloadJson, CancellationToken ct)
    {
        var log = new WebhookDeliveryLog
        {
            WebhookSubscriptionId = subscription.Id,
            EventType = eventType,
            PayloadJson = payloadJson
        };

        try
        {
            var signature = ComputeSignature(payloadJson, subscription.Secret);

            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-ATS-Signature", signature);
            request.Headers.Add("X-ATS-Event", eventType);

            var response = await _http.SendAsync(request, ct);
            log.ResponseStatusCode = (int)response.StatusCode;
            log.Success = response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Webhook delivery failed for subscription {SubscriptionId} ({Url})", subscription.Id, subscription.Url);
        }

        await _uow.Repository<WebhookDeliveryLog>().AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);
    }

    // HMAC-SHA256 over the raw payload body, hex-encoded — the receiver recomputes this with
    // their copy of the shared secret to verify the request actually came from us and wasn't
    // tampered with in transit. Standard pattern (same approach Stripe/GitHub webhooks use).
    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
