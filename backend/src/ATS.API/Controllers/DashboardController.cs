using System.Security.Claims;
using ATS.Application.Common.Interfaces;
using ATS.Application.Features.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>
    /// Aggregated KPIs + chart data for recruiters/HR/admin. Tenant-scoped: Recruiter/HRManager
    /// always see only their own company's data — the companyId claim from their JWT is used,
    /// never a client-supplied value, so one company's recruiter can never query another
    /// company's dashboard. SuperAdmin may omit companyId for a system-wide view.
    /// </summary>
    [HttpGet("recruiter")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetRecruiterDashboard([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var effectiveCompanyId = _currentUser.Role == "SuperAdmin" ? companyId : _currentUser.CompanyId;
        var result = await _mediator.Send(new GetRecruiterDashboardQuery(effectiveCompanyId), ct);
        return Ok(result);
    }

    /// <summary>KPIs for the logged-in candidate.</summary>
    [HttpGet("candidate")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetCandidateDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCandidateDashboardQuery(CurrentUserId), ct);
        return Ok(result);
    }
}
