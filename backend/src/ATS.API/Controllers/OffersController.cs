using System.Security.Claims;
using ATS.Application.Features.Offers.Commands;
using ATS.Application.Features.Offers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OffersController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Extend an offer for an application. Sends an AI-drafted offer email.</summary>
    [HttpPost]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> Create(CreateOfferCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Candidate or recruiter accepts or declines an offer.</summary>
    [HttpPost("{offerId:guid}/respond")]
    [Authorize(Roles = "Candidate,Recruiter,HRManager,SuperAdmin")]

    public async Task<IActionResult> Respond(Guid offerId, [FromBody] bool accept, CancellationToken ct)
    {
        var result = await _mediator.Send(new RespondToOfferCommand(offerId, CurrentUserId, accept), ct);
        return Ok(result);
    }

    /// <summary>The logged-in candidate's or recruiter's active offers.</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Candidate,Recruiter,HRManager,SuperAdmin")]

    public async Task<IActionResult> GetMyOffers(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyOffersQuery(CurrentUserId), ct);
        return Ok(result);
    }
}
