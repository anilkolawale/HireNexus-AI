using ATS.Application.Features.Companies.Commands;
using ATS.Application.Features.Companies.Queries;
using ATS.Application.Features.Departments.Commands;
using ATS.Application.Features.Departments.Queries;
using ATS.Application.Features.Designations.Commands;
using ATS.Application.Features.Designations.Queries;
using ATS.Application.Features.OfficeLocations.Commands;
using ATS.Application.Features.OfficeLocations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompaniesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCompaniesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> Create(CreateCompanyCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> Update(Guid id, UpdateCompanyCommand body, CancellationToken ct)
    {
        var result = await _mediator.Send(body with { Id = id }, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCompanyCommand(id), ct);
        return NoContent();
    }

    [HttpGet("{companyId:guid}/departments")]
    public async Task<IActionResult> GetDepartments(Guid companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDepartmentsByCompanyQuery(companyId), ct);
        return Ok(result);
    }

    [HttpPost("departments")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> CreateDepartment(CreateDepartmentCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPut("departments/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> UpdateDepartment(Guid id, UpdateDepartmentCommand body, CancellationToken ct)
    {
        var result = await _mediator.Send(body with { Id = id }, ct);
        return Ok(result);
    }

    [HttpDelete("departments/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> DeleteDepartment(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDepartmentCommand(id), ct);
        return NoContent();
    }

    [HttpGet("departments/{departmentId:guid}/designations")]
    public async Task<IActionResult> GetDesignations(Guid departmentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDesignationsByDepartmentQuery(departmentId), ct);
        return Ok(result);
    }

    [HttpPost("designations")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> CreateDesignation(CreateDesignationCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPut("designations/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> UpdateDesignation(Guid id, UpdateDesignationCommand body, CancellationToken ct)
    {
        var result = await _mediator.Send(body with { Id = id }, ct);
        return Ok(result);
    }

    [HttpDelete("designations/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> DeleteDesignation(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDesignationCommand(id), ct);
        return NoContent();
    }

    [HttpGet("{companyId:guid}/locations")]
    public async Task<IActionResult> GetLocations(Guid companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOfficeLocationsByCompanyQuery(companyId), ct);
        return Ok(result);
    }

    [HttpPost("locations")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> CreateLocation(CreateOfficeLocationCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPut("locations/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> UpdateLocation(Guid id, UpdateOfficeLocationCommand body, CancellationToken ct)
    {
        var result = await _mediator.Send(body with { Id = id }, ct);
        return Ok(result);
    }

    [HttpDelete("locations/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteOfficeLocationCommand(id), ct);
        return NoContent();
    }

    /// <summary>Upload/replace a company's logo. Accepts PNG/JPG/SVG up to 2MB.</summary>
    [HttpPost("{id:guid}/logo")]
    [Authorize(Roles = "SuperAdmin,HRManager")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var allowedTypes = new[] { "image/png", "image/jpeg", "image/svg+xml" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only PNG, JPG, and SVG files are supported." });

        using var stream = file.OpenReadStream();
        var logoUrl = await _mediator.Send(
            new UploadCompanyLogoCommand(id, stream, file.FileName, file.ContentType), ct);

        return Ok(new { logoUrl });
    }
}
