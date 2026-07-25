using System.Net.Http.Json;
using System.Text.Json;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Enums;
using ATS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Services;

/// <summary>
/// Evaluates automation rules for a company and dispatches configured actions.
/// Called by application command handlers (apply, status change) and Hangfire background jobs.
///
/// Trigger types:
///   MatchScoreAbove:          config: {"minScore": 85}
///   ApplicationStatusChanged: config: {"toStatus": "Shortlisted"}
///   DaysInStageExceeds:       config: {"days": 3, "stage": "Applied"}
///   ApplicationReceived:      config: {} (fires on any new application)
///   CandidateHired:           config: {} (fires when status = Hired)
///
/// Action types:
///   SendEmail:       config: {"subject": "...", "body": "..."}
///   SendNotification:config: {"message": "..."}
///   MoveToStage:     config: {"stage": "Shortlisted"}
///   AssignToRecruiter: config: {"recruiterId": "guid"}
///   CreateTask:      config: {"title": "...", "assignedTo": "HR"}
/// </summary>
public class AutomationEngine : IAutomationEngine
{
    private readonly AtsDbContext _db;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AutomationEngine> _logger;

    public AutomationEngine(
        AtsDbContext db,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<AutomationEngine> logger)
    {
        _db = db;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>Called after an application's AI match score is set.</summary>
    public async Task EvaluateOnApplicationScoredAsync(Guid applicationId, Guid companyId, int score, CancellationToken ct = default)
    {
        var rules = await GetActiveRulesAsync(companyId, AutomationTrigger.MatchScoreAbove, ct);
        foreach (var rule in rules)
        {
            try
            {
                var config = ParseConfig(rule.TriggerConfigJson);
                if (!config.TryGetValue("minScore", out var minScoreRaw)) continue;
                if (!int.TryParse(minScoreRaw.ToString(), out var minScore)) continue;
                if (score < minScore) continue;

                await DispatchActionAsync(rule, applicationId, ct);
                await UpdateRuleStatsAsync(rule, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Automation rule {RuleId} evaluation failed for application {AppId}", rule.Id, applicationId);
            }
        }
    }

    /// <summary>Called when an application's status is changed.</summary>
    public async Task EvaluateOnStatusChangedAsync(Guid applicationId, Guid companyId, string toStatus, CancellationToken ct = default)
    {
        // Fire ApplicationStatusChanged rules
        var statusRules = await GetActiveRulesAsync(companyId, AutomationTrigger.ApplicationStatusChanged, ct);
        foreach (var rule in statusRules)
        {
            try
            {
                var config = ParseConfig(rule.TriggerConfigJson);
                if (config.TryGetValue("toStatus", out var requiredStatus) && requiredStatus.ToString() != toStatus) continue;
                await DispatchActionAsync(rule, applicationId, ct);
                await UpdateRuleStatsAsync(rule, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Automation rule {RuleId} evaluation failed", rule.Id);
            }
        }

        // Fire CandidateHired if applicable
        if (toStatus == "Hired")
        {
            var companies = await _db.Companies.Select(c => c.Id).ToListAsync(ct);
            foreach (var cId in companies)
            {
                var hireRules = await GetActiveRulesAsync(cId, AutomationTrigger.CandidateHired, ct);
                foreach (var rule in hireRules)
                {
                    await DispatchActionAsync(rule, applicationId, ct);
                    await UpdateRuleStatsAsync(rule, ct);
                }
            }
        }
    }

    /// <summary>Hangfire recurring job — checks all applications for SLA stage breaches.</summary>
    public async Task EvaluateDailySlaBatchAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Running daily SLA automation evaluation");

        // Get all company IDs with active SLA rules
        var companiesWithSlaRules = await _db.Set<Domain.Entities.AutomationRule>()
            .Where(r => r.IsEnabled && r.Trigger == AutomationTrigger.DaysInStageExceeds && !r.IsDeleted)
            .Select(r => r.CompanyId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var companyId in companiesWithSlaRules)
        {
            var rules = await GetActiveRulesAsync(companyId, AutomationTrigger.DaysInStageExceeds, ct);
            var applications = await _db.Set<Domain.Entities.Application>()
                .Include(a => a.Job)
                .Where(a => a.Job.CompanyId == companyId && !a.IsDeleted)
                .ToListAsync(ct);

            foreach (var rule in rules)
            {
                var config = ParseConfig(rule.TriggerConfigJson);
                if (!config.TryGetValue("days", out var daysRaw)) continue;
                if (!int.TryParse(daysRaw.ToString(), out var days)) continue;
                var stageName = config.TryGetValue("stage", out var s) ? s.ToString() : null;

                foreach (var app in applications)
                {
                    var statusHistory = await _db.Set<Domain.Entities.ApplicationStatusHistory>()
                        .Where(h => h.ApplicationId == app.Id)
                        .OrderByDescending(h => h.ChangedAtUtc)
                        .FirstOrDefaultAsync(ct);

                    var lastChangeDate = statusHistory?.ChangedAtUtc ?? app.CreatedAtUtc;
                    var daysInStage = (DateTime.UtcNow - lastChangeDate).TotalDays;

                    if (stageName != null && app.Status.ToString() != stageName) continue;
                    if (daysInStage < days) continue;

                    await DispatchActionAsync(rule, app.Id, ct);
                }

                await UpdateRuleStatsAsync(rule, ct);
            }
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task DispatchActionAsync(Domain.Entities.AutomationRule rule, Guid applicationId, CancellationToken ct)
    {
        var actionConfig = ParseConfig(rule.ActionConfigJson);

        switch (rule.Action)
        {
            case AutomationAction.SendEmail:
            {
                var app = await _db.Set<Domain.Entities.Application>()
                    .Include(a => a.Candidate).ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
                if (app is null) break;

                var to = app.Candidate.User.Email;
                var subject = actionConfig.TryGetValue("subject", out var sub) ? sub.ToString()! : $"Update from AI ATS System";
                var body = actionConfig.TryGetValue("body", out var b) ? b.ToString()!
                    : $"<p>Dear {app.Candidate.User.FirstName},</p><p>There is an update on your application.</p>";

                await _emailService.SendAsync(to, subject, body, ct);
                _logger.LogInformation("Automation rule '{Rule}' sent email to {Email}", rule.Name, to);
                break;
            }

            case AutomationAction.SendNotification:
            {
                var app = await _db.Set<Domain.Entities.Application>()
                    .Include(a => a.Candidate)
                    .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
                if (app is null) break;

                var message = actionConfig.TryGetValue("message", out var m) ? m.ToString()! : "Automation rule fired.";
                await _notificationService.NotifyUserAsync(app.Candidate.UserId, rule.Name, message, ct);
                _logger.LogInformation("Automation rule '{Rule}' sent notification to candidate {CandidateId}", rule.Name, app.CandidateId);
                break;
            }

            case AutomationAction.MoveToStage:
            {
                if (!actionConfig.TryGetValue("stage", out var stageRaw)) break;
                if (!Enum.TryParse<ApplicationStatus>(stageRaw.ToString(), out var newStatus)) break;

                var app = await _db.Set<Domain.Entities.Application>()
                    .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
                if (app is null) break;

                app.Status = newStatus;

                _db.Set<Domain.Entities.ApplicationStatusHistory>().Add(new Domain.Entities.ApplicationStatusHistory
                {
                    ApplicationId = applicationId,
                    FromStatus = app.Status,
                    ToStatus = newStatus,
                    ChangedAtUtc = DateTime.UtcNow,
                    ChangedByUserId = Guid.Empty, // System action
                    Notes = $"Auto-moved by automation rule: {rule.Name}"
                });

                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Automation rule '{Rule}' moved application {AppId} to {Stage}", rule.Name, applicationId, newStatus);
                break;
            }

            default:
                _logger.LogInformation("Automation rule '{Rule}' action {Action} not yet implemented in engine", rule.Name, rule.Action);
                break;
        }
    }

    private async Task<List<Domain.Entities.AutomationRule>> GetActiveRulesAsync(Guid companyId, AutomationTrigger trigger, CancellationToken ct)
        => await _db.Set<Domain.Entities.AutomationRule>()
            .Where(r => r.CompanyId == companyId && r.IsEnabled && r.Trigger == trigger && !r.IsDeleted)
            .ToListAsync(ct);

    private async Task UpdateRuleStatsAsync(Domain.Entities.AutomationRule rule, CancellationToken ct)
    {
        rule.ExecutionCount++;
        rule.LastFiredAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static Dictionary<string, object> ParseConfig(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}
