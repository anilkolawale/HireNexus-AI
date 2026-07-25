using ATS.Application.Features.Users.Queries;
using ATS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>List active users by role — powers interviewer/hiring-manager pickers.</summary>
    [HttpGet]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetByRole([FromQuery] UserRoleType role, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUsersByRoleQuery(role), ct);
        return Ok(result);
    }
}
