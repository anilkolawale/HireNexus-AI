using System.Security.Claims;
using ATS.Application.Common.Interfaces;
using ATS.Application.Features.Interviews.Commands;
using ATS.Application.Features.Interviews.Queries;
using ATS.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ATS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAiService _aiService;
    private readonly ICalendarService _calendarService;
    private readonly AtsDbContext _db;

    public InterviewsController(IMediator mediator, IAiService aiService, ICalendarService calendarService, AtsDbContext db)
    {
        _mediator = mediator;
        _aiService = aiService;
        _calendarService = calendarService;
        _db = db;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")!);

    /// <summary>Schedule an interview round for an application. Notifies candidate + interviewer, sends email.</summary>
    [HttpPost("schedule")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> Schedule(ScheduleInterviewCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>All interview rounds + interviews for an application.</summary>
    [HttpGet("application/{applicationId:guid}")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin,Interviewer")]
    public async Task<IActionResult> GetForApplication(Guid applicationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInterviewsForApplicationQuery(applicationId), ct);
        return Ok(result);
    }

    /// <summary>The logged-in interviewer's upcoming schedule.</summary>
    [HttpGet("my-schedule")]
    [Authorize(Roles = "Interviewer,Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> GetMySchedule(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyInterviewsQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Submit interview feedback + result.</summary>
    [HttpPost("{interviewId:guid}/feedback")]
    [Authorize(Roles = "Interviewer,Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> SubmitFeedback(Guid interviewId, SubmitFeedbackCommand body, CancellationToken ct)
    {
        var command = body with { InterviewId = interviewId };
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>AI-generated interview questions tailored to the job + candidate's resume.</summary>
    [HttpGet("application/{applicationId:guid}/ai-questions")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin,Interviewer")]
    public async Task<IActionResult> GetAiQuestions(Guid applicationId, [FromServices] IMediator mediator, CancellationToken ct)
    {
        var rounds = await mediator.Send(new GetInterviewsForApplicationQuery(applicationId), ct);
        var jobTitle = rounds.FirstOrDefault()?.Interviews.FirstOrDefault()?.JobTitle ?? "the role";
        var questions = await _aiService.GenerateInterviewQuestionsAsync(jobTitle, "", "Mid-level", ct);
        return Ok(questions);
    }

    /// <summary>AI summarizes all interviewers' feedback into a hiring recommendation.</summary>
    [HttpPost("{applicationId:guid}/feedback/summarize")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> SummarizeFeedback(Guid applicationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SummarizeFeedbackCommand(applicationId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Downloads an RFC 5545 .ics calendar file for the specified interview.
    /// Works with all mail clients (Gmail, Outlook, Apple Mail) — just open to add to calendar.
    /// </summary>
    [HttpGet("{interviewId:guid}/ics")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin,Interviewer,Candidate")]
    public async Task<IActionResult> DownloadIcs(Guid interviewId, CancellationToken ct)
    {
        var interview = await _db.Interviews
            .Include(i => i.InterviewRound)
                .ThenInclude(r => r.Application)
                    .ThenInclude(a => a.Candidate)
                        .ThenInclude(c => c.User)
            .Include(i => i.Interviewer)
            .FirstOrDefaultAsync(i => i.Id == interviewId, ct);

        if (interview is null) return NotFound();

        var candidate = interview.InterviewRound.Application.Candidate;
        var candidateName = $"{candidate.User.FirstName} {candidate.User.LastName}";
        var candidateEmail = candidate.User.Email;
        var interviewerName = $"{interview.Interviewer.FirstName} {interview.Interviewer.LastName}";
        var interviewerEmail = interview.Interviewer.Email;

        var icsContent = _calendarService.GenerateIcs(
            interview, candidateName, candidateEmail, interviewerName, interviewerEmail);

        var fileName = $"interview-{interview.Id:N}.ics";
        var bytes = System.Text.Encoding.UTF8.GetBytes(icsContent);
        return File(bytes, "text/calendar", fileName);
    }

    /// <summary>Sends .ics calendar invite emails to both candidate and interviewer.</summary>
    [HttpPost("{interviewId:guid}/send-invite")]
    [Authorize(Roles = "Recruiter,HRManager,SuperAdmin")]
    public async Task<IActionResult> SendCalendarInvite(Guid interviewId, CancellationToken ct)
    {
        var interview = await _db.Interviews
            .Include(i => i.InterviewRound)
                .ThenInclude(r => r.Application)
                    .ThenInclude(a => a.Candidate)
                        .ThenInclude(c => c.User)
            .Include(i => i.Interviewer)
            .FirstOrDefaultAsync(i => i.Id == interviewId, ct);

        if (interview is null) return NotFound();

        var candidate = interview.InterviewRound.Application.Candidate;
        await _calendarService.SendCalendarInviteAsync(
            interview,
            $"{candidate.User.FirstName} {candidate.User.LastName}",
            candidate.User.Email,
            $"{interview.Interviewer.FirstName} {interview.Interviewer.LastName}",
            interview.Interviewer.Email,
            ct);

        return Ok(new { message = "Calendar invite sent to candidate and interviewer." });
    }
}
