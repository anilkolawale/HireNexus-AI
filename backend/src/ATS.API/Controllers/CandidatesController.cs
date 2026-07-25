using System.Security.Claims;
using ATS.Application.Features.Candidates.Commands;
using ATS.Application.Features.Candidates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CandidatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CandidatesController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Get the logged-in candidate's profile.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCandidateProfileQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Create/update the logged-in candidate's profile.</summary>
    [HttpPut("me")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> UpdateMyProfile(UpdateCandidateProfileCommand body, CancellationToken ct)
    {
        var command = body with { UserId = CurrentUserId };
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Full resume version history for the logged-in candidate, newest first.</summary>
    [HttpGet("me/resume-history")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetResumeHistory(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetResumeHistoryQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Bulk-import candidates from a CSV (FirstName,LastName,Email,Skills). Skills is semicolon-separated.</summary>
    [HttpPost("bulk-import")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> BulkImport(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No CSV file provided." });

        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(ct);

        var result = await _mediator.Send(new BulkImportCandidatesCommand(content), ct);
        return Ok(result);
    }

    /// <summary>
    /// Search the full candidate database (talent pool), not just applicants to one job.
    /// Recruiter/HRManager/SuperAdmin only.
    /// </summary>
    [HttpGet("talent-pool")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> SearchTalentPool(
        [FromQuery] string? searchTerm,
        [FromQuery] string? skills,
        [FromQuery] int? minExperienceYears,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new SearchTalentPoolQuery(searchTerm, skills, minExperienceYears, pageNumber, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Triggers AI enrichment of a candidate's profile using Gemini.
    /// Generates AiProfileSummary and AiProfileScore from existing profile data.
    /// </summary>
    [HttpPost("{id:guid}/ai-enrich")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> AiEnrich(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new EnrichCandidateProfileCommand(id), ct);
        return Ok(result);
    }
}
