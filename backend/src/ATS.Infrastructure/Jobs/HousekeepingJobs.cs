using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Jobs;

/// <summary>
/// Hangfire scheduled jobs for housekeeping tasks.
/// Jobs are registered in Program.cs using RecurringJob.AddOrUpdate.
/// </summary>
public class HousekeepingJobs
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly ILogger<HousekeepingJobs> _logger;

    public HousekeepingJobs(IUnitOfWork uow, IEmailService email, ILogger<HousekeepingJobs> logger)
    {
        _uow = uow;
        _email = email;
        _logger = logger;
    }

    /// <summary>
    /// Purges expired refresh tokens nightly at 02:00 UTC.
    /// Keeps the RefreshTokens table lean and removes stale sessions.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
    public async Task PurgeExpiredRefreshTokensAsync()
    {
        var cutoff = DateTime.UtcNow;
        var repo = _uow.Repository<RefreshToken>();

        var expired = await repo.Query()
            .Where(t => t.ExpiresAtUtc < cutoff || t.RevokedAtUtc.HasValue)
            .ToListAsync();

        if (!expired.Any())
        {
            _logger.LogInformation("Refresh token cleanup: nothing to purge");
            return;
        }

        foreach (var token in expired)
            repo.Remove(token);

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Purged {Count} expired refresh tokens", expired.Count);
    }

    /// <summary>
    /// Purges email OTP verification tokens older than 24 hours.
    /// Runs hourly to keep the EmailVerificationToken table clean.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
    public async Task PurgeExpiredEmailTokensAsync()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var repo = _uow.Repository<EmailVerificationToken>();

        var expired = await repo.Query()
            .Where(t => t.CreatedAtUtc < cutoff || t.VerifiedAtUtc.HasValue)
            .ToListAsync();

        if (!expired.Any())
        {
            _logger.LogInformation("Email token cleanup: nothing to purge");
            return;
        }

        foreach (var token in expired)
            repo.Remove(token);

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Purged {Count} expired email verification tokens", expired.Count);
    }

    /// <summary>
    /// Flags jobs that have passed their closing date as Closed.
    /// Runs daily at midnight UTC.
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task CloseExpiredJobsAsync()
    {
        var repo = _uow.Repository<Job>();
        var today = DateTime.UtcNow.Date;

        var expired = await repo.Query()
            .Where(j => j.Status == ATS.Domain.Enums.JobStatus.Published
                     && j.ClosingDate.HasValue
                     && j.ClosingDate.Value.Date < today)
            .ToListAsync();

        if (!expired.Any())
        {
            _logger.LogInformation("Job expiry check: no jobs to close");
            return;
        }

        foreach (var job in expired)
            job.Status = ATS.Domain.Enums.JobStatus.Closed;

        await _uow.SaveChangesAsync();
        _logger.LogInformation("Auto-closed {Count} expired jobs", expired.Count);
    }

    /// <summary>
    /// Sends 24-hour interview reminder emails to interviewers.
    /// Runs every hour — finds interviews scheduled 23–25h from now with no reminder sent yet.
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task SendInterviewRemindersAsync()
    {
        var windowStart = DateTime.UtcNow.AddHours(23);
        var windowEnd   = DateTime.UtcNow.AddHours(25);

        var upcoming = await _uow.Repository<Interview>()
            .Query()
            .Include(i => i.Interviewer)
            .Include(i => i.InterviewRound)
                .ThenInclude(r => r.Application)
                    .ThenInclude(a => a.Job)
            .Where(i =>
                i.ScheduledAtUtc >= windowStart &&
                i.ScheduledAtUtc <= windowEnd &&
                i.ReminderSentAtUtc == null &&
                i.Result == ATS.Domain.Enums.InterviewResultStatus.Pending)
            .ToListAsync();

        if (!upcoming.Any())
        {
            _logger.LogInformation("Interview reminders: none in 23–25h window");
            return;
        }

        _logger.LogInformation("Sending {Count} interview reminders", upcoming.Count);

        foreach (var interview in upcoming)
        {
            try
            {
                var jobTitle = interview.InterviewRound?.Application?.Job?.Title ?? "the position";
                var dateStr  = interview.ScheduledAtUtc.ToString("dddd, MMMM d 'at' h:mm tt 'UTC'");
                var meetLink = string.IsNullOrWhiteSpace(interview.MeetingLink)
                    ? "<em>Contact your coordinator for the meeting link.</em>"
                    : $"<a href=\"{interview.MeetingLink}\" style=\"color:#6366f1\">Join Meeting →</a>";

                var subject = $"⏰ Interview Tomorrow: {jobTitle}";
                var html =
                    $"<div style=\"font-family:Inter,sans-serif;max-width:560px;margin:0 auto\">" +
                    $"<div style=\"background:linear-gradient(135deg,#6366f1,#10b981);padding:32px;border-radius:16px 16px 0 0\">" +
                    $"<h1 style=\"color:#fff;margin:0;font-size:22px\">Interview Reminder</h1>" +
                    $"<p style=\"color:rgba(255,255,255,.8);margin:8px 0 0\">Your interview is tomorrow</p>" +
                    $"</div>" +
                    $"<div style=\"background:#0f172a;padding:32px;border-radius:0 0 16px 16px;color:#e2e8f0\">" +
                    $"<p>You have an interview scheduled for <strong style=\"color:#a5b4fc\">{dateStr}</strong>.</p>" +
                    $"<table style=\"width:100%;border-collapse:collapse;margin:16px 0\">" +
                    $"<tr><td style=\"padding:8px 0;color:#94a3b8\">Role</td><td style=\"color:#f1f5f9;font-weight:600\">{jobTitle}</td></tr>" +
                    $"<tr><td style=\"padding:8px 0;color:#94a3b8\">Duration</td><td style=\"color:#f1f5f9\">{interview.DurationMinutes} minutes</td></tr>" +
                    $"</table>" +
                    $"<div style=\"margin:24px 0\">{meetLink}</div>" +
                    $"<p style=\"color:#64748b;font-size:13px\">Sent by HireIQ — AI Recruitment Platform</p>" +
                    $"</div></div>";

                await _email.SendAsync(interview.Interviewer.Email, subject, html);

                // Mark reminder sent to prevent duplicate emails on next run
                interview.ReminderSentAtUtc = DateTime.UtcNow;
                _logger.LogInformation("Reminder sent for interview {Id}", interview.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder for interview {Id}", interview.Id);
            }
        }

        await _uow.SaveChangesAsync();
    }
}
