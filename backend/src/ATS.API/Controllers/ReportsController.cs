using ATS.Application.Common.Interfaces;
using ATS.Application.Features.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATS.API.Controllers;

// Tenant isolation: every report is scoped to the caller's own company via the companyId
// claim in their JWT (ICurrentUserService.CompanyId), never a client-supplied query param.
// Only SuperAdmin may pass an explicit companyId (or omit it for a system-wide report) —
// this is what stops one company's Recruiter from ever querying another company's reports
// by simply changing a query string value.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IReportExportService _exportService;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(IMediator mediator, IReportExportService exportService, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _exportService = exportService;
        _currentUser = currentUser;
    }

    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";

    private Guid? EffectiveCompanyId(Guid? clientSupplied) =>
        _currentUser.Role == "SuperAdmin" ? clientSupplied : _currentUser.CompanyId;

    // ---- Hiring Report ----

    [HttpGet("hiring")]
    public async Task<IActionResult> GetHiringReport([FromQuery] Guid? companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHiringReportQuery(EffectiveCompanyId(companyId), from, to), ct);
        return Ok(result);
    }

    [HttpGet("hiring/export/excel")]
    public async Task<IActionResult> ExportHiringExcel([FromQuery] Guid? companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHiringReportQuery(EffectiveCompanyId(companyId), from, to), ct);
        var bytes = _exportService.ExportToExcel(result.Title, result.Rows);
        return File(bytes, ExcelContentType, "hiring-report.xlsx");
    }

    [HttpGet("hiring/export/pdf")]
    public async Task<IActionResult> ExportHiringPdf([FromQuery] Guid? companyId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHiringReportQuery(EffectiveCompanyId(companyId), from, to), ct);
        var bytes = _exportService.ExportToPdf(result.Title, result.Rows);
        return File(bytes, PdfContentType, "hiring-report.pdf");
    }

    // ---- Recruiter Performance Report ----

    [HttpGet("recruiter-performance")]
    public async Task<IActionResult> GetRecruiterPerformance([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRecruiterPerformanceReportQuery(EffectiveCompanyId(companyId)), ct);
        return Ok(result);
    }

    [HttpGet("recruiter-performance/export/excel")]
    public async Task<IActionResult> ExportRecruiterPerformanceExcel([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRecruiterPerformanceReportQuery(EffectiveCompanyId(companyId)), ct);
        var bytes = _exportService.ExportToExcel(result.Title, result.Rows);
        return File(bytes, ExcelContentType, "recruiter-performance-report.xlsx");
    }

    [HttpGet("recruiter-performance/export/pdf")]
    public async Task<IActionResult> ExportRecruiterPerformancePdf([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRecruiterPerformanceReportQuery(EffectiveCompanyId(companyId)), ct);
        var bytes = _exportService.ExportToPdf(result.Title, result.Rows);
        return File(bytes, PdfContentType, "recruiter-performance-report.pdf");
    }

    // ---- Candidate Report ----
    // Not company-scoped: candidates are a shared talent pool across the platform (they can
    // apply to jobs at multiple companies), matching how the Candidate Report/Talent Pool
    // search are designed elsewhere. No client-supplied filter to worry about here.

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidateReport(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCandidateReportQuery(), ct);
        return Ok(result);
    }

    [HttpGet("candidates/export/excel")]
    public async Task<IActionResult> ExportCandidateExcel(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCandidateReportQuery(), ct);
        var bytes = _exportService.ExportToExcel(result.Title, result.Rows);
        return File(bytes, ExcelContentType, "candidate-report.xlsx");
    }

    [HttpGet("candidates/export/pdf")]
    public async Task<IActionResult> ExportCandidatePdf(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCandidateReportQuery(), ct);
        var bytes = _exportService.ExportToPdf(result.Title, result.Rows);
        return File(bytes, PdfContentType, "candidate-report.pdf");
    }

    // ---- Department Report ----

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartmentReport([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDepartmentReportQuery(EffectiveCompanyId(companyId)), ct);
        return Ok(result);
    }

    [HttpGet("departments/export/excel")]
    public async Task<IActionResult> ExportDepartmentExcel([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDepartmentReportQuery(EffectiveCompanyId(companyId)), ct);
        var bytes = _exportService.ExportToExcel(result.Title, result.Rows);
        return File(bytes, ExcelContentType, "department-report.xlsx");
    }

    [HttpGet("departments/export/pdf")]
    public async Task<IActionResult> ExportDepartmentPdf([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDepartmentReportQuery(EffectiveCompanyId(companyId)), ct);
        var bytes = _exportService.ExportToPdf(result.Title, result.Rows);
        return File(bytes, PdfContentType, "department-report.pdf");
    }

    // ---- Job Report ----

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobReport([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetJobReportQuery(EffectiveCompanyId(companyId)), ct);
        return Ok(result);
    }

    [HttpGet("jobs/export/excel")]
    public async Task<IActionResult> ExportJobExcel([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetJobReportQuery(EffectiveCompanyId(companyId)), ct);
        var bytes = _exportService.ExportToExcel(result.Title, result.Rows);
        return File(bytes, ExcelContentType, "job-report.xlsx");
    }

    [HttpGet("jobs/export/pdf")]
    public async Task<IActionResult> ExportJobPdf([FromQuery] Guid? companyId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetJobReportQuery(EffectiveCompanyId(companyId)), ct);
        var bytes = _exportService.ExportToPdf(result.Title, result.Rows);
        return File(bytes, PdfContentType, "job-report.pdf");
    }
}
