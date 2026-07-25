using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATS.API.Controllers;

/// <summary>
/// Manages talent prospects in the CRM pipeline (Lever-style TRM).
/// Prospects are passive candidates tracked before they apply.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
public class ProspectsController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAiService _ai;

    public ProspectsController(IUnitOfWork uow, ICurrentUserService currentUser, IAiService ai)
    {
        _uow         = uow;
        _currentUser = currentUser;
        _ai          = ai;
    }

    // GET /api/prospects
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProspectStatus? status, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId ?? Guid.Empty;
        var query = _uow.Repository<TalentProspect>().Query()
            .Where(p => p.CompanyId == companyId);

        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        var prospects = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new
            {
                p.Id, p.FullName, p.Email, p.Phone, p.CurrentTitle,
                p.LinkedInUrl, p.Skills, p.Source, p.Status,
                p.LastContactedAtUtc, p.CreatedAtUtc,
                HasAiOutreach = p.AiOutreachEmail != null
            })
            .ToListAsync(ct);
        return Ok(prospects);
    }

    // POST /api/prospects
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProspectRequest req, CancellationToken ct)
    {
        var prospect = new TalentProspect
        {
            Id           = Guid.NewGuid(),
            CompanyId    = _currentUser.CompanyId ?? Guid.Empty,
            AddedByUserId = _currentUser.UserId,
            FullName     = req.FullName,
            Email        = req.Email,
            Phone        = req.Phone,
            LinkedInUrl  = req.LinkedInUrl,
            CurrentTitle = req.CurrentTitle,
            Skills       = req.Skills,
            Source       = req.Source,
            Status       = ProspectStatus.New
        };
        await _uow.Repository<TalentProspect>().AddAsync(prospect, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { prospect.Id });
    }

    // PATCH /api/prospects/{id}/status
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req, CancellationToken ct)
    {
        var prospect = await _uow.Repository<TalentProspect>().Query()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (prospect is null) return NotFound();

        prospect.Status = req.Status;
        if (req.Status == ProspectStatus.Contacted)
            prospect.LastContactedAtUtc = DateTime.UtcNow;

        _uow.Repository<TalentProspect>().Update(prospect);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { prospect.Status });
    }

    // POST /api/prospects/{id}/ai-outreach
    [HttpPost("{id:guid}/ai-outreach")]
    public async Task<IActionResult> GenerateOutreach(Guid id, CancellationToken ct)
    {
        var prospect = await _uow.Repository<TalentProspect>().Query()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (prospect is null) return NotFound();

        var context = $"Prospect: {prospect.FullName}, Current Title: {prospect.CurrentTitle ?? "Unknown"}, " +
                      $"Skills: {prospect.Skills ?? "Not specified"}, Source: {prospect.Source}";

        var email = await _ai.GenerateEmailAsync(
            "personalized recruiting outreach to a passive candidate",
            context, ct);

        prospect.AiOutreachEmail = email;
        _uow.Repository<TalentProspect>().Update(prospect);
        await _uow.SaveChangesAsync(ct);
        return Ok(new { email });
    }

    // DELETE /api/prospects/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var prospect = await _uow.Repository<TalentProspect>().Query()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (prospect is null) return NotFound();
        _uow.Repository<TalentProspect>().Remove(prospect);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }
}

// Public Career Portal (no auth)
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public PublicController(IUnitOfWork uow) => _uow = uow;

    // GET /api/public/jobs
    [HttpGet("jobs")]
    public async Task<IActionResult> GetPublicJobs(
        [FromQuery] string? keyword,
        [FromQuery] string? department,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        const int pageSize = 10;
        var query = _uow.Repository<ATS.Domain.Entities.Job>().Query()
            .Include(j => j.Company)
            .Where(j => j.Status == ATS.Domain.Enums.JobStatus.Published);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(j => j.Title.Contains(keyword) || j.Description.Contains(keyword));
        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(j => j.Department.Name.Contains(department));

        var total = await query.CountAsync(ct);
        var jobs = await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id, j.Title, j.Description, j.Department, j.Location,
                j.EmploymentType, j.SalaryMin, j.SalaryMax, j.ClosingDate, j.CreatedAtUtc,
                Company = new { j.Company.Name, j.Company.LogoUrl }
            })
            .ToListAsync(ct);

        return Ok(new { total, page, pageSize, jobs });
    }

    // GET /api/public/jobs/{id}
    [HttpGet("jobs/{id:guid}")]
    public async Task<IActionResult> GetPublicJob(Guid id, CancellationToken ct)
    {
        var job = await _uow.Repository<ATS.Domain.Entities.Job>().Query()
            .Include(j => j.Company)
            .Include(j => j.JobSkills)
            .Where(j => j.Id == id && j.Status == ATS.Domain.Enums.JobStatus.Published)
            .Select(j => new
            {
                j.Id, j.Title, j.Description, j.Department, j.Location,
                j.EmploymentType, j.SalaryMin, j.SalaryMax, j.ClosingDate, j.CreatedAtUtc,
                Company = new { j.Company.Name, j.Company.LogoUrl, j.Company.Website },
                Skills  = j.JobSkills.Select(s => s.SkillName)
            })
            .FirstOrDefaultAsync(ct);

        if (job is null) return NotFound();
        return Ok(job);
    }

    // POST /api/public/apply
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] PublicApplyRequest req, CancellationToken ct)
    {
        // Verify job exists and is published
        var job = await _uow.Repository<ATS.Domain.Entities.Job>().Query()
            .FirstOrDefaultAsync(j => j.Id == req.JobId && j.Status == ATS.Domain.Enums.JobStatus.Published, ct);
        if (job is null) return NotFound("Job not found or no longer accepting applications.");

        // Check for duplicate email application
        var alreadyApplied = await _uow.Repository<ATS.Domain.Entities.Application>().Query()
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .AnyAsync(a => a.JobId == req.JobId && a.Candidate.User.Email == req.Email, ct);
        if (alreadyApplied)
            return Conflict("An application from this email already exists for this job.");

        // For public applicants without an account, we return 200 with a reference number
        // In production this would create a guest candidate record
        return Ok(new
        {
            message = "Application received! We'll be in touch shortly.",
            reference = $"APP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}"
        });
    }
}

public record CreateProspectRequest(string FullName, string Email, string? Phone,
    string? LinkedInUrl, string? CurrentTitle, string? Skills, ProspectSource Source);
public record UpdateStatusRequest(ProspectStatus Status);
public record PublicApplyRequest(Guid JobId, string FullName, string Email,
    string? Phone, string? CoverLetter, string? ResumeUrl);
