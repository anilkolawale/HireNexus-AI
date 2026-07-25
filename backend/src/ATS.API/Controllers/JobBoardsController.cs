using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATS.API.Controllers;

/// <summary>
/// Manages one-click publishing of jobs to external job boards.
/// Each board is credential-gated — boards without API keys return stub responses.
/// </summary>
[ApiController]
[Route("api/job-boards")]
[Authorize]
public class JobBoardsController : ControllerBase
{
    private readonly AtsDbContext _db;
    private readonly IJobBoardPublisher _publisher;

    public JobBoardsController(AtsDbContext db, IJobBoardPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    /// <summary>Returns all supported boards and their configuration status.</summary>
    [HttpGet("boards")]
    public IActionResult GetBoards()
    {
        var boards = _publisher.GetSupportedBoards();
        return Ok(boards);
    }

    /// <summary>Publishes a job to the specified board. Creates a JobBoardPosting record.</summary>
    [HttpPost("{jobId:guid}/publish/{board}")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> Publish(Guid jobId, string board, CancellationToken ct)
    {
        var job = await _db.Jobs
            .Include(j => j.Company)
            .Include(j => j.Department)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job is null) return NotFound(new { message = "Job not found." });

        // Check for existing active posting on this board
        var existing = await _db.JobBoardPostings
            .FirstOrDefaultAsync(p => p.JobId == jobId && p.Board == board && p.Status == JobBoardPostingStatus.Active, ct);

        if (existing is not null)
            return Conflict(new { message = $"Job is already actively posted on {board}." });

        // Create pending record
        var posting = new JobBoardPosting
        {
            JobId = jobId,
            Board = board,
            Status = JobBoardPostingStatus.Pending
        };
        _db.JobBoardPostings.Add(posting);
        await _db.SaveChangesAsync(ct);

        // Publish to board
        var externalId = await _publisher.PublishAsync(job, board, ct);

        posting.ExternalPostingId = externalId;
        posting.Status = externalId is not null ? JobBoardPostingStatus.Active : JobBoardPostingStatus.Failed;
        posting.ErrorMessage = externalId is null ? "Publisher returned no external ID. Check board credentials in appsettings." : null;
        posting.PostedAtUtc = externalId is not null ? DateTime.UtcNow : null;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            posting.Id,
            posting.Board,
            Status = posting.Status.ToString(),
            posting.ExternalPostingId,
            posting.PostedAtUtc,
            posting.ErrorMessage
        });

    }

    /// <summary>Lists all job board postings for a job.</summary>
    [HttpGet("{jobId:guid}/postings")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetPostings(Guid jobId, CancellationToken ct)
    {
        var postings = await _db.JobBoardPostings
            .Where(p => p.JobId == jobId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new
            {
                p.Id,
                p.Board,
                Status = p.Status.ToString(),
                p.ExternalPostingId,
                p.PostedAtUtc,
                p.ExpiresAtUtc,
                p.ErrorMessage
            })
            .ToListAsync(ct);

        return Ok(postings);

    }

    /// <summary>Unpublishes a job from a specific board and marks it Closed.</summary>
    [HttpDelete("{jobId:guid}/postings/{board}")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> Unpublish(Guid jobId, string board, CancellationToken ct)
    {
        var posting = await _db.JobBoardPostings
            .FirstOrDefaultAsync(p => p.JobId == jobId && p.Board == board && p.Status == JobBoardPostingStatus.Active, ct);

        if (posting is null)
            return NotFound(new { message = $"No active posting found for {board}." });

        if (!string.IsNullOrWhiteSpace(posting.ExternalPostingId))
        {
            await _publisher.UnpublishAsync(posting.ExternalPostingId, board, ct);
        }

        posting.Status = JobBoardPostingStatus.Closed;
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = $"Job unpublished from {board}." });
    }
}
