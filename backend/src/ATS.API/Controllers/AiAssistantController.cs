using System.Security.Claims;
using ATS.Application.DTOs.AiAssistant;
using ATS.Application.Features.AiAssistant.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

public record GenerateJobDescRequest(string Title, string Department, string ExperienceLevel, string KeySkills);

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
public class AiAssistantController : ControllerBase
{
    private readonly IMediator _mediator;

    public AiAssistantController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Chat with the recruitment assistant. Grounded in live open-jobs + top-ranked applications data.</summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat(ChatRequestDto request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChatWithAssistantCommand(CurrentUserId, request.Message, request.History), ct);
        return Ok(result);
    }

    /// <summary>Generate an AI-drafted job description for a new or existing job posting.</summary>
    [HttpPost("generate-job-description")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GenerateJobDescription([FromBody] GenerateJobDescRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateJobDescriptionCommand(req.Title, req.Department, req.ExperienceLevel, req.KeySkills), ct);
        return Ok(new { description = result });
    }
}
