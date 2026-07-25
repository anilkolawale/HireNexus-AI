using ATS.Domain.Entities;
using ATS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATS.API.Controllers;

/// <summary>
/// Manages blind screening configuration per job.
/// When enabled, candidate PII (name, photo, demographics) is masked in ranked candidate views.
/// Designed to reduce unconscious bias in initial screening (EEOC best practice).
/// </summary>
[ApiController]
[Route("api/blind-screening")]
[Authorize]
public class BlindScreeningController : ControllerBase
{
    private readonly AtsDbContext _db;

    public BlindScreeningController(AtsDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets the blind screening configuration for a job. Returns defaults if not yet configured.</summary>
    [HttpGet("jobs/{jobId:guid}")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetConfig(Guid jobId, CancellationToken ct)
    {
        var config = await _db.BlindScreeningConfigs.FirstOrDefaultAsync(c => c.JobId == jobId, ct);

        // Return default config if not yet configured
        if (config is null)
        {
            return Ok(new
            {
                JobId = jobId,
                IsEnabled = false,
                HideName = true,
                HidePhoto = true,
                HideGender = false,
                HideEthnicity = false,
                HideAge = false,
                IsConfigured = false
            });
        }

        return Ok(new
        {
            config.Id,
            config.JobId,
            config.IsEnabled,
            config.HideName,
            config.HidePhoto,
            config.HideGender,
            config.HideEthnicity,
            config.HideAge,
            IsConfigured = true
        });
    }

    /// <summary>Creates or updates the blind screening config for a job. Recruiter/HRManager/SuperAdmin.</summary>
    [HttpPut("jobs/{jobId:guid}")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> UpsertConfig(Guid jobId, BlindScreeningRequest request, CancellationToken ct)

    {
        var config = await _db.BlindScreeningConfigs.FirstOrDefaultAsync(c => c.JobId == jobId, ct);

        if (config is null)
        {
            config = new BlindScreeningConfig { JobId = jobId };
            _db.BlindScreeningConfigs.Add(config);
        }

        config.IsEnabled = request.IsEnabled;
        config.HideName = request.HideName;
        config.HidePhoto = request.HidePhoto;
        config.HideGender = request.HideGender;
        config.HideEthnicity = request.HideEthnicity;
        config.HideAge = request.HideAge;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            config.Id,
            config.JobId,
            config.IsEnabled,
            config.HideName,
            config.HidePhoto,
            config.HideGender,
            config.HideEthnicity,
            config.HideAge,
            message = config.IsEnabled
                ? "Blind screening ENABLED. Candidate names and selected fields will be masked in recruiter view."
                : "Blind screening DISABLED. All candidate information is visible."
        });
    }
}

public record BlindScreeningRequest(
    bool IsEnabled,
    bool HideName = true,
    bool HidePhoto = true,
    bool HideGender = false,
    bool HideEthnicity = false,
    bool HideAge = false
);
