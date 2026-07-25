using System.Security.Claims;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATS.API.Controllers;

/// <summary>
/// CRUD API for company-scoped no-code automation rules.
/// Rules are evaluated by the AutomationEngine on application events (score, status change, SLA breach).
/// </summary>
[ApiController]
[Route("api/automations")]
[Authorize]
public class AutomationsController : ControllerBase
{
    private readonly AtsDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AutomationsController(AtsDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Lists all automation rules for the current company.</summary>
    [HttpGet]
    [Authorize(Roles = "HRManager,SuperAdmin,Recruiter")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        if (companyId is null) return Forbid();

        var rules = await _db.AutomationRules
            .Where(r => r.CompanyId == companyId.Value && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.IsEnabled,
                Trigger = r.Trigger.ToString(),
                r.TriggerConfigJson,
                Action = r.Action.ToString(),
                r.ActionConfigJson,
                r.ExecutionCount,
                r.LastFiredAtUtc,
                r.CreatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(rules);
    }

    /// <summary>Creates a new automation rule for the current company.</summary>
    [HttpPost]
    [Authorize(Roles = "HRManager,SuperAdmin")]
    public async Task<IActionResult> Create(CreateAutomationRuleRequest request, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        if (companyId is null) return Forbid();

        if (!Enum.TryParse<AutomationTrigger>(request.Trigger, true, out var trigger))
            return BadRequest(new { message = $"Invalid trigger: {request.Trigger}. Valid values: {string.Join(", ", Enum.GetNames<AutomationTrigger>())}" });

        if (!Enum.TryParse<AutomationAction>(request.Action, true, out var action))
            return BadRequest(new { message = $"Invalid action: {request.Action}. Valid values: {string.Join(", ", Enum.GetNames<AutomationAction>())}" });

        var rule = new AutomationRule
        {
            CompanyId = companyId.Value,
            Name = request.Name,
            Description = request.Description,
            IsEnabled = request.IsEnabled,
            Trigger = trigger,
            TriggerConfigJson = request.TriggerConfigJson ?? "{}",
            Action = action,
            ActionConfigJson = request.ActionConfigJson ?? "{}"
        };

        _db.AutomationRules.Add(rule);
        await _db.SaveChangesAsync(ct);

        return Created($"/api/automations/{rule.Id}", new { rule.Id, rule.Name, message = "Automation rule created." });
    }

    /// <summary>Enables or disables an automation rule.</summary>
    [HttpPatch("{id:guid}/toggle")]
    [Authorize(Roles = "HRManager,SuperAdmin")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var rule = await _db.AutomationRules.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId && !r.IsDeleted, ct);
        if (rule is null) return NotFound();

        rule.IsEnabled = !rule.IsEnabled;
        await _db.SaveChangesAsync(ct);

        return Ok(new { rule.Id, rule.Name, rule.IsEnabled, message = rule.IsEnabled ? "Rule enabled." : "Rule disabled." });
    }

    /// <summary>Deletes an automation rule (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HRManager,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var rule = await _db.AutomationRules.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId && !r.IsDeleted, ct);
        if (rule is null) return NotFound();

        rule.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Automation rule deleted." });
    }

    /// <summary>Returns available trigger and action types with descriptions and example configs.</summary>
    [HttpGet("meta")]
    public IActionResult GetMeta()
    {
        var triggers = new[]
        {
            new { Name = "MatchScoreAbove", Description = "Fires when a candidate's AI match score exceeds a threshold", ExampleConfig = "{\"minScore\": 85}" },
            new { Name = "ApplicationStatusChanged", Description = "Fires when an application moves to a specific stage", ExampleConfig = "{\"toStatus\": \"Shortlisted\"}" },
            new { Name = "DaysInStageExceeds", Description = "Fires when an application is stuck in a stage for too long", ExampleConfig = "{\"days\": 3, \"stage\": \"Applied\"}" },
            new { Name = "ApplicationReceived", Description = "Fires when any new application is submitted", ExampleConfig = "{}" },
            new { Name = "CandidateHired", Description = "Fires when a candidate's status is set to Hired", ExampleConfig = "{}" },
        };

        var actions = new[]
        {
            new { Name = "SendEmail", Description = "Sends an email to the candidate", ExampleConfig = "{\"subject\": \"Update on your application\", \"body\": \"<p>Hello! We have an update...</p>\"}" },
            new { Name = "SendNotification", Description = "Sends an in-app notification to the candidate", ExampleConfig = "{\"message\": \"You have been shortlisted!\"}" },
            new { Name = "MoveToStage", Description = "Moves the application to a specified stage", ExampleConfig = "{\"stage\": \"Shortlisted\"}" },
            new { Name = "AssignToRecruiter", Description = "Assigns the application to a specific recruiter", ExampleConfig = "{\"recruiterId\": \"guid-here\"}" },
            new { Name = "CreateTask", Description = "Creates a task for the HR team", ExampleConfig = "{\"title\": \"Follow up with candidate\", \"assignedTo\": \"HR\"}" },
        };

        var stages = Enum.GetNames<ApplicationStatus>();

        return Ok(new { triggers, actions, stages });
    }
}

public record CreateAutomationRuleRequest(
    string Name,
    string? Description,
    string Trigger,
    string? TriggerConfigJson,
    string Action,
    string? ActionConfigJson,
    bool IsEnabled = true
);
