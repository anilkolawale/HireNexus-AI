using System.Security.Claims;
using ATS.Application.Features.Privacy.Commands;
using ATS.Application.Features.Privacy.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Candidate")]
public class PrivacyController : ControllerBase
{
    private readonly IMediator _mediator;

    public PrivacyController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>GDPR data export — everything the platform holds about the logged-in candidate.</summary>
    [HttpGet("my-data")]
    public async Task<IActionResult> ExportMyData(CancellationToken ct)
    {
        var result = await _mediator.Send(new ExportMyDataQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>
    /// GDPR right to erasure. Requires the exact confirmation phrase "DELETE MY ACCOUNT".
    /// Anonymizes PII and deactivates the account; application/interview history is retained
    /// (anonymized) for the employer's audit/compliance needs.
    /// </summary>
    [HttpPost("delete-my-account")]
    public async Task<IActionResult> DeleteMyAccount([FromBody] DeleteAccountRequest body, CancellationToken ct)
    {
        await _mediator.Send(new DeleteMyAccountCommand(CurrentUserId, body.ConfirmationPhrase), ct);
        return NoContent();
    }

    public record DeleteAccountRequest(string ConfirmationPhrase);
}
