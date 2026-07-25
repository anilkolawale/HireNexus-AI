using System.Security.Claims;
using ATS.Application.Features.Sessions.Commands;
using ATS.Application.Features.Sessions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

// "Sessions" here means active refresh tokens — each one roughly corresponds to a device/
// browser that's stayed logged in. Available to every role, not just admins: this is a
// personal-security feature ("what's logged into my account"), same as any major platform.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SessionsController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    public record SessionsRequest(string? CurrentRefreshToken);

    /// <summary>Active sessions (devices/browsers) for the logged-in user, newest-used first.</summary>
    [HttpPost("mine")]
    public async Task<IActionResult> GetMySessions(SessionsRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMySessionsQuery(CurrentUserId, body.CurrentRefreshToken), ct);
        return Ok(result);
    }

    /// <summary>Revoke one specific session by its id.</summary>
    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        await _mediator.Send(new RevokeSessionCommand(sessionId, CurrentUserId), ct);
        return NoContent();
    }

    public record RevokeOthersRequest(string CurrentRefreshToken);

    /// <summary>Revoke every session except the one making this request ("log out everywhere else").</summary>
    [HttpPost("revoke-others")]
    public async Task<IActionResult> RevokeOthers(RevokeOthersRequest body, CancellationToken ct)
    {
        var count = await _mediator.Send(new RevokeAllOtherSessionsCommand(CurrentUserId, body.CurrentRefreshToken), ct);
        return Ok(new { revokedCount = count });
    }
}
