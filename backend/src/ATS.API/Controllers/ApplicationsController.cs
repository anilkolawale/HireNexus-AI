using System.Security.Claims;
using ATS.Application.Features.Applications.Commands;
using ATS.Application.Features.Applications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

public record CompareCandidatesRequest(IReadOnlyList<Guid> ApplicationIds, Guid JobId);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationsController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Candidate applies to a job. Runs AI match scoring synchronously.</summary>
    [HttpPost("apply/{jobId:guid}")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Apply(Guid jobId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApplyToJobCommand(CurrentUserId, jobId), ct);
        return Ok(result);
    }

    /// <summary>The logged-in candidate's own applications with status + AI match details.</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetMyApplications(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyApplicationsQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Recruiter / HR Manager view: all candidate applications across all pipeline stages.</summary>
    [HttpGet("pipeline")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin,Interviewer,Candidate")]
    public async Task<IActionResult> GetAllPipeline(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllPipelineApplicationsQuery(), ct);
        return Ok(result);
    }


    /// <summary>Recruiter view: candidates for a job, ranked by AI match score.</summary>
    [HttpGet("job/{jobId:guid}/ranked")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin,Interviewer")]
    public async Task<IActionResult> GetRankedForJob(Guid jobId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRankedApplicationsForJobQuery(jobId), ct);
        return Ok(result);
    }

    /// <summary>Move an application through the recruitment workflow (triggers a notification).</summary>
    [HttpPatch("{applicationId:guid}/status")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin,Interviewer")]
    public async Task<IActionResult> ChangeStatus(Guid applicationId, ChangeApplicationStatusCommand body, CancellationToken ct)
    {
        var command = body with { ApplicationId = applicationId, ChangedByUserId = CurrentUserId };
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/skill-gap")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetSkillGap(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSkillGapQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("compare")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> Compare([FromBody] CompareCandidatesRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompareCandidatesQuery(request.ApplicationIds, request.JobId), ct);
        return Ok(result);
    }
}
