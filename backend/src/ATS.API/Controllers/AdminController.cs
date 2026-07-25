using ATS.Application.Features.Admin.Commands;
using ATS.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    /// <summary>System-wide KPIs: total users/companies/jobs/applications, users by role.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery(), ct);
        return Ok(result);
    }

    /// <summary>Paginated audit trail, optionally filtered by entity type or user.</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityName, [FromQuery] Guid? userId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery(entityName, userId, pageNumber, pageSize), ct);
        return Ok(result);
    }

    /// <summary>All users across the system, with role/company/active-status for management.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? searchTerm, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllUsersQuery(searchTerm), ct);
        return Ok(result);
    }

    /// <summary>Activate or deactivate a user account.</summary>
    [HttpPatch("users/{userId:guid}/active-status")]
    public async Task<IActionResult> SetUserActiveStatus(Guid userId, [FromBody] bool isActive, CancellationToken ct)
    {
        await _mediator.Send(new SetUserActiveStatusCommand(userId, isActive), ct);
        return NoContent();
    }
}
