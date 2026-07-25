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
/// API for video interview and coding assessment management.
/// Supports async video question banks and HackerRank coding test invitations.
/// </summary>
[ApiController]
[Route("api/assessments")]
[Authorize]
public class AssessmentsController : ControllerBase
{
    private readonly AtsDbContext _db;
    private readonly IAssessmentService _assessmentService;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUser;

    public AssessmentsController(AtsDbContext db, IAssessmentService assessmentService, IEmailService emailService, ICurrentUserService currentUser)
    {
        _db = db;
        _assessmentService = assessmentService;
        _emailService = emailService;
        _currentUser = currentUser;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    // ── Templates ────────────────────────────────────────────────────────────

    /// <summary>Creates an assessment template (video questions + optional HackerRank test) for a job.</summary>
    [HttpPost("templates")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> CreateTemplate(CreateAssessmentTemplateRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<AssessmentType>(request.Type, true, out var type))
            return BadRequest(new { message = "Invalid assessment type. Valid: Video, CodingTest, Mixed" });

        var template = new AssessmentTemplate
        {
            JobId = request.JobId,
            Title = request.Title,
            Type = type,
            DurationMinutes = request.DurationMinutes,
            Instructions = request.Instructions,
            HackerRankTestId = request.HackerRankTestId
        };

        foreach (var (q, i) in request.Questions.Select((q, i) => (q, i)))
        {
            template.Questions.Add(new VideoQuestion
            {
                QuestionText = q.QuestionText,
                ThinkTimeSecs = q.ThinkTimeSecs,
                RecordingTimeSecs = q.RecordingTimeSecs,
                Order = i + 1
            });
        }

        _db.AssessmentTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return Created($"/api/assessments/templates/{template.Id}", new { template.Id, template.Title });
    }

    /// <summary>Lists all assessment templates for a job.</summary>
    [HttpGet("templates/job/{jobId:guid}")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetTemplatesForJob(Guid jobId, CancellationToken ct)
    {
        var templates = await _db.AssessmentTemplates
            .Include(t => t.Questions.OrderBy(q => q.Order))
            .Where(t => t.JobId == jobId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.Id,
                t.Title,
                Type = t.Type.ToString(),
                t.DurationMinutes,
                t.Instructions,
                t.HackerRankTestId,
                QuestionCount = t.Questions.Count,
                Questions = t.Questions.OrderBy(q => q.Order).Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.ThinkTimeSecs,
                    q.RecordingTimeSecs,
                    q.Order
                })
            })
            .ToListAsync(ct);

        return Ok(templates);
    }

    // ── Assignments ───────────────────────────────────────────────────────────

    /// <summary>Assigns an assessment template to a candidate application. Sends HackerRank invite if applicable.</summary>
    [HttpPost("assign")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> Assign(AssignAssessmentRequest request, CancellationToken ct)
    {
        var template = await _db.AssessmentTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && !t.IsDeleted, ct);
        if (template is null) return NotFound(new { message = "Assessment template not found." });

        var application = await _db.Applications
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);
        if (application is null) return NotFound(new { message = "Application not found." });

        // Check if already assigned
        var existing = await _db.CandidateAssessments
            .AnyAsync(a => a.ApplicationId == request.ApplicationId && a.TemplateId == request.TemplateId, ct);
        if (existing)
            return Conflict(new { message = "This assessment is already assigned to the candidate." });

        var candidateEmail = application.Candidate.User.Email;
        string? hackerRankUrl = null;

        // Send HackerRank invite if it's a coding test
        if ((template.Type == AssessmentType.CodingTest || template.Type == AssessmentType.Mixed)
            && !string.IsNullOrWhiteSpace(template.HackerRankTestId))
        {
            hackerRankUrl = await _assessmentService.CreateHackerRankInviteAsync(candidateEmail, template.HackerRankTestId, ct);
        }

        var assessment = new CandidateAssessment
        {
            ApplicationId = request.ApplicationId,
            TemplateId = request.TemplateId,
            Status = AssessmentStatus.Pending,
            SentAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(request.ExpiryDays ?? 7),
            HackerRankInviteUrl = hackerRankUrl
        };

        _db.CandidateAssessments.Add(assessment);
        await _db.SaveChangesAsync(ct);

        // Notify candidate
        var subject = $"📋 Assessment Assigned: {template.Title}";
        var bodyHtml = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px;'>
    <h2 style='color: #4f46e5;'>Assessment Assigned</h2>
    <p>Dear {application.Candidate.User.FirstName} {application.Candidate.User.LastName},</p>
    <p>You have been assigned an assessment as part of your job application process.</p>
    <p><strong>Assessment:</strong> {template.Title}<br/>
    <strong>Duration:</strong> {template.DurationMinutes} minutes<br/>
    <strong>Deadline:</strong> {assessment.ExpiresAtUtc:dddd, MMMM d yyyy}</p>
    {(hackerRankUrl != null ? $"<p><a href='{hackerRankUrl}' style='background:#4f46e5;color:white;padding:10px 20px;text-decoration:none;border-radius:6px;'>Start Coding Assessment</a></p>" : "")}
    {(template.Type == AssessmentType.Video || template.Type == AssessmentType.Mixed ? "<p>Please log in to the recruitment portal to complete your video interview questions.</p>" : "")}
    {(template.Instructions != null ? $"<p><strong>Instructions:</strong> {template.Instructions}</p>" : "")}
</div>";

        await _emailService.SendAsync(candidateEmail, subject, bodyHtml, ct);

        return Ok(new
        {
            assessment.Id,
            assessment.ApplicationId,
            assessment.TemplateId,
            Status = assessment.Status.ToString(),
            assessment.SentAtUtc,
            assessment.ExpiresAtUtc,
            assessment.HackerRankInviteUrl,
            message = "Assessment assigned and candidate notified via email."
        });
    }

    /// <summary>Candidate: Lists all assessments assigned to the current user's applications.</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetMyAssessments(CancellationToken ct)
    {
        var userId = CurrentUserId;
        var assessments = await _db.CandidateAssessments
            .Include(a => a.Template)
                .ThenInclude(t => t.Questions.OrderBy(q => q.Order))
            .Include(a => a.Application)
                .ThenInclude(app => app.Job)
            .Include(a => a.VideoResponses)
            .Where(a => a.Application.Candidate.UserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.SentAtUtc)
            .Select(a => new
            {
                a.Id,
                TemplateName = a.Template.Title,
                AssessmentType = a.Template.Type.ToString(),
                JobTitle = a.Application.Job.Title,
                Status = a.Status.ToString(),
                a.SentAtUtc,
                a.ExpiresAtUtc,
                a.CompletedAtUtc,
                a.HackerRankInviteUrl,
                a.Template.DurationMinutes,
                a.Template.Instructions,
                Questions = a.Template.Questions.OrderBy(q => q.Order).Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.ThinkTimeSecs,
                    q.RecordingTimeSecs,
                    q.Order,
                    IsAnswered = a.VideoResponses.Any(r => r.QuestionId == q.Id && r.BlobVideoUrl != null)
                })
            })
            .ToListAsync(ct);

        return Ok(assessments);
    }

    /// <summary>Candidate: Submits a video response (blob URL) for a question.</summary>
    [HttpPost("{assessmentId:guid}/responses")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> SubmitResponse(Guid assessmentId, SubmitVideoResponseRequest request, CancellationToken ct)
    {
        var assessment = await _db.CandidateAssessments
            .Include(a => a.Application)
                .ThenInclude(app => app.Candidate)
            .Include(a => a.VideoResponses)
            .Include(a => a.Template)
                .ThenInclude(t => t.Questions)
            .FirstOrDefaultAsync(a => a.Id == assessmentId, ct);

        if (assessment is null) return NotFound();

        if (assessment.Application.Candidate.UserId != CurrentUserId) return Forbid();

        if (assessment.Status == AssessmentStatus.Expired)
            return BadRequest(new { message = "This assessment has expired." });

        if (assessment.Status == AssessmentStatus.Completed)
            return BadRequest(new { message = "This assessment is already completed." });

        // Upsert response (allow re-recording within deadline)
        var existingResponse = assessment.VideoResponses.FirstOrDefault(r => r.QuestionId == request.QuestionId);
        if (existingResponse is not null)
        {
            existingResponse.BlobVideoUrl = request.BlobVideoUrl;
            existingResponse.DurationSeconds = request.DurationSeconds;
            existingResponse.SubmittedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _db.VideoResponses.Add(new VideoResponse
            {
                AssessmentId = assessmentId,
                QuestionId = request.QuestionId,
                BlobVideoUrl = request.BlobVideoUrl,
                DurationSeconds = request.DurationSeconds,
                SubmittedAtUtc = DateTime.UtcNow
            });
        }

        // Check if all questions answered → mark complete
        var totalQuestions = assessment.Template.Questions.Count;
        var answeredAfterSave = assessment.VideoResponses.Count(r => r.BlobVideoUrl != null);
        var newlyAnswered = existingResponse is null ? answeredAfterSave + 1 : answeredAfterSave;

        if (newlyAnswered >= totalQuestions)
        {
            assessment.Status = AssessmentStatus.Completed;
            assessment.CompletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            assessment.Status = AssessmentStatus.InProgress;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            assessment.Id,
            Status = assessment.Status.ToString(),
            assessment.CompletedAtUtc,
            message = assessment.Status == AssessmentStatus.Completed
                ? "Assessment completed! All questions answered."
                : $"Response saved. {totalQuestions - newlyAnswered} question(s) remaining."
        });
    }

    /// <summary>Recruiter: Views assessment results and video responses for a candidate.</summary>
    [HttpGet("{assessmentId:guid}/results")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetResults(Guid assessmentId, CancellationToken ct)
    {
        var assessment = await _db.CandidateAssessments
            .Include(a => a.Template).ThenInclude(t => t.Questions)
            .Include(a => a.VideoResponses)
            .Include(a => a.Application).ThenInclude(app => app.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == assessmentId, ct);

        if (assessment is null) return NotFound();

        var result = new
        {
            assessment.Id,
            CandidateName = $"{assessment.Application.Candidate.User.FirstName} {assessment.Application.Candidate.User.LastName}",
            TemplateName = assessment.Template.Title,
            Status = assessment.Status.ToString(),
            assessment.CompletedAtUtc,
            assessment.HackerRankInviteUrl,
            Responses = assessment.Template.Questions.OrderBy(q => q.Order).Select(q =>
            {
                var resp = assessment.VideoResponses.FirstOrDefault(r => r.QuestionId == q.Id);
                return new
                {
                    q.Order,
                    q.QuestionText,
                    q.RecordingTimeSecs,
                    IsAnswered = resp?.BlobVideoUrl is not null,
                    VideoUrl = resp?.BlobVideoUrl,
                    DurationSeconds = resp?.DurationSeconds,
                    SubmittedAt = resp?.SubmittedAtUtc
                };
            })
        };

        return Ok(result);
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public record CreateAssessmentTemplateRequest(
    Guid JobId,
    string Title,
    string Type,
    int DurationMinutes,
    string? Instructions,
    string? HackerRankTestId,
    List<VideoQuestionRequest> Questions
);

public record VideoQuestionRequest(
    string QuestionText,
    int ThinkTimeSecs = 30,
    int RecordingTimeSecs = 120
);

public record AssignAssessmentRequest(
    Guid ApplicationId,
    Guid TemplateId,
    int? ExpiryDays = 7
);

public record SubmitVideoResponseRequest(
    Guid QuestionId,
    string BlobVideoUrl,
    int? DurationSeconds = null
);
