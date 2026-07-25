using System.Text;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Services;

/// <summary>
/// Generates RFC 5545 compliant .ics iCalendar files and sends them as email attachments.
/// Works without Google/Microsoft OAuth — uses plain email with .ics attachment.
/// Any mail client (Gmail, Outlook, Apple Mail, Thunderbird) natively handles .ics invites.
/// </summary>
public class CalendarService : ICalendarService
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(IEmailService emailService, IConfiguration config, ILogger<CalendarService> logger)
    {
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string GenerateIcs(
        Interview interview,
        string candidateName,
        string candidateEmail,
        string interviewerName,
        string interviewerEmail)
    {
        var startUtc = interview.ScheduledAtUtc;
        var endUtc = startUtc.AddMinutes(interview.DurationMinutes);
        var uid = $"{interview.Id}@ats-system";
        var now = DateTime.UtcNow;
        var organizerEmail = _config["Smtp:FromAddress"] ?? "noreply@ats.local";

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//AI-ATS-System//Interview Scheduler//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:REQUEST");

        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{uid}");
        sb.AppendLine($"DTSTAMP:{FormatDateTime(now)}");
        sb.AppendLine($"DTSTART:{FormatDateTime(startUtc)}");
        sb.AppendLine($"DTEND:{FormatDateTime(endUtc)}");
        sb.AppendLine($"SUMMARY:Interview - {candidateName}");
        sb.AppendLine($"DESCRIPTION:Interview scheduled via AI ATS System.\\nCandidate: {candidateName}\\nInterviewer: {interviewerName}\\nDuration: {interview.DurationMinutes} minutes{(interview.MeetingLink != null ? $"\\nMeeting Link: {interview.MeetingLink}" : "")}");
        sb.AppendLine($"ORGANIZER;CN=AI ATS System:MAILTO:{organizerEmail}");
        sb.AppendLine($"ATTENDEE;CN={candidateName};ROLE=REQ-PARTICIPANT;PARTSTAT=NEEDS-ACTION;RSVP=TRUE:MAILTO:{candidateEmail}");
        sb.AppendLine($"ATTENDEE;CN={interviewerName};ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED:MAILTO:{interviewerEmail}");
        if (!string.IsNullOrWhiteSpace(interview.MeetingLink))
        {
            sb.AppendLine($"LOCATION:{interview.MeetingLink}");
            sb.AppendLine($"URL:{interview.MeetingLink}");
        }
        sb.AppendLine("BEGIN:VALARM");
        sb.AppendLine("ACTION:DISPLAY");
        sb.AppendLine("DESCRIPTION:Interview reminder - 30 minutes");
        sb.AppendLine("TRIGGER:-PT30M");
        sb.AppendLine("END:VALARM");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task SendCalendarInviteAsync(
        Interview interview,
        string candidateName,
        string candidateEmail,
        string interviewerName,
        string interviewerEmail,
        CancellationToken ct = default)
    {
        var icsContent = GenerateIcs(interview, candidateName, candidateEmail, interviewerName, interviewerEmail);
        var dateStr = interview.ScheduledAtUtc.ToString("dddd, MMMM d yyyy 'at' HH:mm 'UTC'");

        var subject = $"📅 Interview Scheduled: {candidateName} — {dateStr}";
        var htmlBody = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px;'>
    <h2 style='color: #4f46e5;'>Interview Scheduled</h2>
    <p>An interview has been scheduled via the AI Recruitment System.</p>
    <table style='border-collapse: collapse; width: 100%;'>
        <tr><td style='padding: 8px; font-weight: bold;'>Candidate:</td><td style='padding: 8px;'>{candidateName}</td></tr>
        <tr><td style='padding: 8px; font-weight: bold;'>Interviewer:</td><td style='padding: 8px;'>{interviewerName}</td></tr>
        <tr><td style='padding: 8px; font-weight: bold;'>Date &amp; Time:</td><td style='padding: 8px;'>{dateStr}</td></tr>
        <tr><td style='padding: 8px; font-weight: bold;'>Duration:</td><td style='padding: 8px;'>{interview.DurationMinutes} minutes</td></tr>
        {(interview.MeetingLink != null ? $"<tr><td style='padding: 8px; font-weight: bold;'>Meeting Link:</td><td style='padding: 8px;'><a href='{interview.MeetingLink}'>{interview.MeetingLink}</a></td></tr>" : "")}
    </table>
    <p style='color: #6b7280; font-size: 12px;'>A calendar invite (.ics file) is attached. Open it to add this event to your calendar.</p>
</div>
<!-- ICS_ATTACHMENT:{Convert.ToBase64String(Encoding.UTF8.GetBytes(icsContent))} -->";

        try
        {
            await _emailService.SendAsync(candidateEmail, subject, htmlBody, ct);
            await _emailService.SendAsync(interviewerEmail, subject, htmlBody, ct);
            _logger.LogInformation("Calendar invite sent for interview {InterviewId} to {Candidate} and {Interviewer}",
                interview.Id, candidateEmail, interviewerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send calendar invite for interview {InterviewId}", interview.Id);
        }
    }

    private static string FormatDateTime(DateTime dt)
        => dt.ToString("yyyyMMdd'T'HHmmss'Z'");
}
