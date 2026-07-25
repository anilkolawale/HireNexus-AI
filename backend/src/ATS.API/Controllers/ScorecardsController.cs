using System.Security.Claims;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScorecardsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAiService _ai;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    public ScorecardsController(IUnitOfWork uow, IAiService ai)
    {
        _uow = uow;
        _ai  = ai;
    }

    // GET /api/scorecards/templates?jobId=
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] Guid jobId, CancellationToken ct)
    {
        var templates = await _uow.Repository<ScorecardTemplate>().Query()
            .Include(t => t.Criteria.OrderBy(c => c.Order))
            .Where(t => t.JobId == jobId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id, t.Name, t.JobId, t.CreatedAtUtc,
                Criteria = t.Criteria.Select(c => new { c.Id, c.Name, c.Description, c.Weight, c.Order })
            })
            .ToListAsync(ct);
        return Ok(templates);
    }

    // POST /api/scorecards/templates
    [HttpPost("templates")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest req, CancellationToken ct)
    {
        var template = new ScorecardTemplate
        {
            Id      = Guid.NewGuid(),
            JobId   = req.JobId,
            Name    = req.Name,
        };
        int order = 0;
        foreach (var c in req.Criteria ?? new())
        {
            template.Criteria.Add(new ScorecardCriterion
            {
                Id          = Guid.NewGuid(),
                Name        = c.Name,
                Description = c.Description,
                Weight      = c.Weight,
                Order       = order++
            });
        }
        await _uow.Repository<ScorecardTemplate>().AddAsync(template, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { template.Id });
    }

    // POST /api/scorecards/templates/{id}/ai-generate
    [HttpPost("templates/{id:guid}/ai-generate")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> AiGenerateCriteria(Guid id, CancellationToken ct)
    {
        var template = await _uow.Repository<ScorecardTemplate>().Query()
            .Include(t => t.Job)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template is null) return NotFound();

        var prompt =
            $"Generate 5 structured interview scorecard evaluation criteria for a '{template.Job.Title}' role. " +
            $"Return ONLY a valid JSON array (no markdown): " +
            $"[{{\"name\":\"string\",\"description\":\"string\",\"weight\":20}}]. " +
            $"Weights must sum to 100. Focus on the most critical competencies for this role.";

        var json = await _ai.ChatAsync(prompt, "{}", ct);

        template.AiGeneratedCriteria = json;
        _uow.Repository<ScorecardTemplate>().Update(template);
        await _uow.SaveChangesAsync(ct);

        return Ok(new { criteria = json });
    }

    // GET /api/scorecards?interviewId=
    [HttpGet]
    public async Task<IActionResult> GetScorecards([FromQuery] Guid interviewId, CancellationToken ct)
    {
        var scorecards = await _uow.Repository<InterviewScorecard>().Query()
            .Include(s => s.Interviewer)
            .Include(s => s.Scores).ThenInclude(sc => sc.Criterion)
            .Where(s => s.InterviewId == interviewId)
            .Select(s => new
            {
                s.Id, s.InterviewId, s.IsSubmitted, s.Decision, s.OverallComment, s.SubmittedAtUtc,
                Interviewer = new { s.Interviewer.Id, Name = s.Interviewer.FirstName + " " + s.Interviewer.LastName, s.Interviewer.Email },
                Scores = s.Scores.Select(sc => new
                {
                    sc.CriterionId, CriterionName = sc.Criterion.Name,
                    sc.Rating, sc.Comment
                })
            })
            .ToListAsync(ct);
        return Ok(scorecards);
    }

    // POST /api/scorecards
    [HttpPost]
    public async Task<IActionResult> SubmitScorecard([FromBody] SubmitScorecardRequest req, CancellationToken ct)
    {
        var scorecard = new InterviewScorecard
        {
            Id           = Guid.NewGuid(),
            TemplateId   = req.TemplateId,
            InterviewId  = req.InterviewId,
            InterviewerId = CurrentUserId,
            Decision     = req.Decision,
            OverallComment = req.OverallComment,
            IsSubmitted  = true,
            SubmittedAtUtc = DateTime.UtcNow,
        };
        foreach (var s in req.Scores ?? new())
        {
            scorecard.Scores.Add(new ScorecardScore
            {
                Id          = Guid.NewGuid(),
                CriterionId = s.CriterionId,
                Rating      = s.Rating,
                Comment     = s.Comment
            });
        }
        await _uow.Repository<InterviewScorecard>().AddAsync(scorecard, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { scorecard.Id });
    }
}

public record CreateTemplateRequest(Guid JobId, string Name, List<CriterionInput>? Criteria);
public record CriterionInput(string Name, string? Description, int Weight);
public record SubmitScorecardRequest(Guid TemplateId, Guid InterviewId,
    ATS.Domain.Enums.ScorecardDecision? Decision, string? OverallComment, List<ScoreInput>? Scores);
public record ScoreInput(Guid CriterionId, int Rating, string? Comment);
