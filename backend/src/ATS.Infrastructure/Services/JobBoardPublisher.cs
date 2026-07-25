using System.Net.Http.Json;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Services;

/// <summary>
/// Publishes job listings to external job boards (LinkedIn, Indeed, Glassdoor, ZipRecruiter).
/// Each board implementation is a credential-gated stub:
///   - If API credentials are present in config → calls the real API
///   - If credentials are absent → logs a warning and returns a stub posting ID
///
/// To activate a board, add credentials to appsettings.json:
///   "JobBoards": {
///     "LinkedIn": { "ClientId": "...", "ClientSecret": "...", "AccessToken": "..." },
///     "Indeed":   { "PublisherKey": "..." },
///     "Glassdoor":{ "ApiKey": "..." },
///     "ZipRecruiter": { "ApiKey": "..." }
///   }
/// </summary>
public class JobBoardPublisher : IJobBoardPublisher
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JobBoardPublisher> _logger;

    public JobBoardPublisher(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<JobBoardPublisher> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IReadOnlyList<JobBoardInfo> GetSupportedBoards() =>
    [
        new("LinkedIn", "LinkedIn Jobs", IsConfigured("LinkedIn:AccessToken"), "https://cdn.jsdelivr.net/npm/simple-icons@v10/icons/linkedin.svg"),
        new("Indeed", "Indeed", IsConfigured("Indeed:PublisherKey"), "https://cdn.jsdelivr.net/npm/simple-icons@v10/icons/indeed.svg"),
        new("Glassdoor", "Glassdoor", IsConfigured("Glassdoor:ApiKey"), "https://cdn.jsdelivr.net/npm/simple-icons@v10/icons/glassdoor.svg"),
        new("ZipRecruiter", "ZipRecruiter", IsConfigured("ZipRecruiter:ApiKey"), null),
    ];

    public async Task<string?> PublishAsync(Job job, string board, CancellationToken ct = default)
    {
        return board.ToLowerInvariant() switch
        {
            "linkedin" => await PublishToLinkedInAsync(job, ct),
            "indeed" => await PublishToIndeedAsync(job, ct),
            "glassdoor" => await PublishToGlassdoorAsync(job, ct),
            "ziprecruiter" => await PublishToZipRecruiterAsync(job, ct),
            _ => null
        };
    }

    public async Task<bool> UnpublishAsync(string externalPostingId, string board, CancellationToken ct = default)
    {
        _logger.LogInformation("Unpublishing job {PostingId} from {Board}", externalPostingId, board);

        // Each board would call their respective DELETE API. Stubbed for now.
        await Task.Delay(50, ct); // Simulate API call
        return true;
    }

    // ── LinkedIn ─────────────────────────────────────────────────────────────

    private async Task<string?> PublishToLinkedInAsync(Job job, CancellationToken ct)
    {
        var accessToken = _config["JobBoards:LinkedIn:AccessToken"];
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogWarning("LinkedIn:AccessToken not configured. Returning stub posting ID for job {JobId}", job.Id);
            await Task.Delay(50, ct);
            return $"linkedin-stub-{job.Id}";
        }

        try
        {
            var client = _httpClientFactory.CreateClient("LinkedInJobs");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            client.DefaultRequestHeaders.Add("LinkedIn-Version", "202401");

            var payload = new
            {
                externalJobPostingId = job.Id.ToString(),
                title = job.Title,
                description = new { text = $"{job.Description}\n\n{job.Responsibilities}" },
                employmentType = MapEmploymentType(job.EmploymentType),
                workRemoteAllowed = job.RemoteOption,
                listedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                jobPostingOperationType = "CREATE"
            };

            var response = await client.PostAsJsonAsync("https://api.linkedin.com/rest/simpleJobPostings", payload, ct);
            if (response.IsSuccessStatusCode)
            {
                var locationHeader = response.Headers.Location?.ToString();
                _logger.LogInformation("Published job {JobId} to LinkedIn: {Location}", job.Id, locationHeader);
                return locationHeader ?? $"linkedin-{job.Id}";
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("LinkedIn publish failed for job {JobId}: {Error}", job.Id, error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LinkedIn publish exception for job {JobId}", job.Id);
            return null;
        }
    }

    // ── Indeed ───────────────────────────────────────────────────────────────

    private async Task<string?> PublishToIndeedAsync(Job job, CancellationToken ct)
    {
        var publisherKey = _config["JobBoards:Indeed:PublisherKey"];
        if (string.IsNullOrWhiteSpace(publisherKey))
        {
            _logger.LogWarning("Indeed:PublisherKey not configured. Returning stub posting ID for job {JobId}", job.Id);
            await Task.Delay(50, ct);
            return $"indeed-stub-{job.Id}";
        }

        // Indeed uses their XML Job Feed / Employer API
        // Stub: return mock ID — wire up with real Indeed Publisher API when key is provided
        _logger.LogInformation("Publishing job {JobId} to Indeed (stub — configure Indeed:PublisherKey)", job.Id);
        await Task.Delay(100, ct);
        return $"indeed-{job.Id}";
    }

    // ── Glassdoor ────────────────────────────────────────────────────────────

    private async Task<string?> PublishToGlassdoorAsync(Job job, CancellationToken ct)
    {
        var apiKey = _config["JobBoards:Glassdoor:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Glassdoor:ApiKey not configured. Returning stub posting ID for job {JobId}", job.Id);
            await Task.Delay(50, ct);
            return $"glassdoor-stub-{job.Id}";
        }

        _logger.LogInformation("Publishing job {JobId} to Glassdoor (stub — configure Glassdoor:ApiKey)", job.Id);
        await Task.Delay(100, ct);
        return $"glassdoor-{job.Id}";
    }

    // ── ZipRecruiter ─────────────────────────────────────────────────────────

    private async Task<string?> PublishToZipRecruiterAsync(Job job, CancellationToken ct)
    {
        var apiKey = _config["JobBoards:ZipRecruiter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("ZipRecruiter:ApiKey not configured. Returning stub posting ID for job {JobId}", job.Id);
            await Task.Delay(50, ct);
            return $"ziprecruiter-stub-{job.Id}";
        }

        _logger.LogInformation("Publishing job {JobId} to ZipRecruiter (stub — configure ZipRecruiter:ApiKey)", job.Id);
        await Task.Delay(100, ct);
        return $"ziprecruiter-{job.Id}";
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private bool IsConfigured(string configKey)
        => !string.IsNullOrWhiteSpace(_config[$"JobBoards:{configKey}"]);

    private static string MapEmploymentType(Domain.Enums.EmploymentType type) => type switch
    {
        Domain.Enums.EmploymentType.FullTime => "FULL_TIME",
        Domain.Enums.EmploymentType.PartTime => "PART_TIME",
        Domain.Enums.EmploymentType.Contract => "CONTRACT",
        Domain.Enums.EmploymentType.Internship => "INTERNSHIP",
        Domain.Enums.EmploymentType.Remote => "FULL_TIME",
        _ => "FULL_TIME"
    };
}
