using ATS.Application.Common.Interfaces;
using ATS.Application.Features.Jobs.Commands;
using ATS.Application.Features.Jobs.Queries;
using ATS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAiService _aiService;
    private readonly ICurrentUserService _currentUser;

    public JobsController(IMediator mediator, IAiService aiService, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _aiService = aiService;
        _currentUser = currentUser;
    }

    /// <summary>List jobs with search, filter, and pagination.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? searchTerm,
        [FromQuery] JobStatus? status,
        [FromQuery] string? location,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetJobsQuery(searchTerm, status, location, pageNumber, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new job posting. Recruiter/HRManager/SuperAdmin only. CompanyId and
    /// CreatedByRecruiterId are always taken from the caller's JWT, never the request body —
    /// otherwise a recruiter could spoof another company's identity or forge attribution.
    /// SuperAdmin (no fixed company of their own) may pass an explicit CompanyId.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> CreateJob(CreateJobCommand command, CancellationToken ct)
    {
        var effectiveCompanyId = _currentUser.Role == "SuperAdmin" && command.CompanyId != Guid.Empty
            ? command.CompanyId
            : _currentUser.CompanyId ?? command.CompanyId;

        var secured = command with
        {
            CompanyId = effectiveCompanyId,
            CreatedByRecruiterId = _currentUser.UserId!.Value
        };

        var result = await _mediator.Send(secured, ct);
        return Ok(result);
    }

    public record GenerateDescriptionRequest(string Title, string Department, string ExperienceLevel, string KeySkills);

    /// <summary>AI-generate a professional job description from a few prompts (title/department/experience/skills).</summary>
    [HttpPost("generate-description")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GenerateDescription(GenerateDescriptionRequest request, CancellationToken ct)
    {
        var description = await _aiService.GenerateJobDescriptionAsync(
            request.Title, request.Department, request.ExperienceLevel, request.KeySkills, ct);
        return Ok(new { description });
    }

    private bool IsSuperAdmin => _currentUser.Role == "SuperAdmin";
    private Guid CompanyIdOrEmpty => _currentUser.CompanyId ?? Guid.Empty;

    public record UpdateJobRequest(
        string Title, string Description, string? Responsibilities, string? Benefits,
        string ExperienceRequired, decimal SalaryMin, decimal SalaryMax, string Location,
        EmploymentType EmploymentType, List<string> Skills);

    /// <summary>Edit a job's details. Only the owning company (or SuperAdmin) may edit it.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> UpdateJob(Guid id, UpdateJobRequest body, CancellationToken ct)
    {
        var command = new UpdateJobCommand(
            id, CompanyIdOrEmpty, IsSuperAdmin, body.Title, body.Description, body.Responsibilities,
            body.Benefits, body.ExperienceRequired, body.SalaryMin, body.SalaryMax, body.Location,
            body.EmploymentType, body.Skills);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Move a job from Draft to Published — makes it visible on the public job board. Fires a job.published webhook.</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> PublishJob(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new PublishJobCommand(id, CompanyIdOrEmpty, IsSuperAdmin), ct);
        return NoContent();
    }

    /// <summary>Close a job (stops accepting new applications). Fires a job.closed webhook.</summary>
    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> CloseJob(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CloseJobCommand(id, CompanyIdOrEmpty, IsSuperAdmin), ct);
        return NoContent();
    }

    /// <summary>Delete a job. Blocked if it already has applications — close it instead.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> DeleteJob(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteJobCommand(id, CompanyIdOrEmpty, IsSuperAdmin), ct);
        return NoContent();
    }

    /// <summary>Duplicate a job as a new Draft — useful for near-identical repeat postings.</summary>
    [HttpPost("{id:guid}/duplicate")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> DuplicateJob(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DuplicateJobCommand(id, CompanyIdOrEmpty, IsSuperAdmin, _currentUser.UserId!.Value), ct);
        return Ok(result);
    }
}
