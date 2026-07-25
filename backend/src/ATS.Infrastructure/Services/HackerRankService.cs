using System.Net.Http.Json;
using ATS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Services;

/// <summary>
/// Integrates with HackerRank's test invitation API to send coding assessments to candidates.
/// Config-gated: add HackerRank:ApiKey to appsettings.json to activate live invites.
/// Returns a stub URL in development when the API key is absent.
///
/// HackerRank API docs: https://apidocs.hackerrank.com/
/// Endpoint: POST /x/v3/tests/{test_id}/candidates/invite
/// </summary>
public class HackerRankService : IAssessmentService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HackerRankService> _logger;

    public HackerRankService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<HackerRankService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> CreateHackerRankInviteAsync(string candidateEmail, string testId, CancellationToken ct = default)
    {
        var apiKey = _config["HackerRank:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("HackerRank:ApiKey not configured. Returning stub invite URL for candidate {Email}", candidateEmail);
            await Task.Delay(50, ct);
            return $"https://www.hackerrank.com/test/{testId}/candidate/{Guid.NewGuid():N}?source=ats-stub";
        }

        try
        {
            var baseUrl = _config["HackerRank:BaseUrl"] ?? "https://www.hackerrank.com/x/api/v3/";
            var client = _httpClientFactory.CreateClient("HackerRank");
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {apiKey}");

            var payload = new
            {
                test_id = testId,
                emails = new[] { candidateEmail },
                send_email = true
            };

            var response = await client.PostAsJsonAsync($"tests/{testId}/candidates/invite", payload, ct);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HackerRankInviteResponse>(cancellationToken: ct);
                var inviteUrl = result?.candidates?.FirstOrDefault()?.invite_url
                    ?? $"https://www.hackerrank.com/test/{testId}";

                _logger.LogInformation("HackerRank invite created for {Email} on test {TestId}: {Url}", candidateEmail, testId, inviteUrl);
                return inviteUrl;
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("HackerRank invite failed for {Email}: {Error}", candidateEmail, error);
            return $"https://www.hackerrank.com/test/{testId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HackerRank service exception for {Email}", candidateEmail);
            return $"https://www.hackerrank.com/test/{testId}";
        }
    }

    private sealed class HackerRankInviteResponse
    {
        public List<HackerRankCandidate>? candidates { get; set; }
    }

    private sealed class HackerRankCandidate
    {
        public string? email { get; set; }
        public string? invite_url { get; set; }
    }
}
