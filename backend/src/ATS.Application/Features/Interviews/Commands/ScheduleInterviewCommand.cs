using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Interviews;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Interviews.Commands;

public record ScheduleInterviewCommand(
    Guid ApplicationId,
    string RoundName,
    int SequenceOrder,
    Guid InterviewerId,
    DateTime ScheduledAtUtc,
    int DurationMinutes,
    string? MeetingLink) : IRequest<InterviewDto>;

public class ScheduleInterviewCommandValidator : AbstractValidator<ScheduleInterviewCommand>
{
    public ScheduleInterviewCommandValidator()
    {
        RuleFor(x => x.RoundName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScheduledAtUtc).GreaterThan(DateTime.UtcNow).WithMessage("Interview must be scheduled in the future.");
        RuleFor(x => x.DurationMinutes).GreaterThan(0).LessThanOrEqualTo(480);
    }
}

public class ScheduleInterviewCommandHandler : IRequestHandler<ScheduleInterviewCommand, InterviewDto>
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;
    private readonly IEmailService _email;

    public ScheduleInterviewCommandHandler(IUnitOfWork uow, INotificationService notifications, IEmailService email)
    {
        _uow = uow;
        _notifications = notifications;
        _email = email;
    }

    public async Task<InterviewDto> Handle(ScheduleInterviewCommand request, CancellationToken ct)
    {
        var application = await _uow.Repository<Domain.Entities.Application>().Query()
            .Include(a => a.Job)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Application), request.ApplicationId);

        var interviewer = await _uow.Repository<User>().GetByIdAsync(request.InterviewerId, ct)
            ?? throw new NotFoundException(nameof(User), request.InterviewerId);

        // Reuse an existing round with the same name for this application, or create one.
        var round = await _uow.Repository<InterviewRound>().Query()
            .FirstOrDefaultAsync(r => r.ApplicationId == request.ApplicationId && r.RoundName == request.RoundName, ct);

        if (round is null)
        {
            round = new InterviewRound
            {
                ApplicationId = request.ApplicationId,
                RoundName = request.RoundName,
                SequenceOrder = request.SequenceOrder
            };
            await _uow.Repository<InterviewRound>().AddAsync(round, ct);
        }

        var interview = new Interview
        {
            InterviewRoundId = round.Id,
            InterviewRound = round,
            InterviewerId = request.InterviewerId,
            ScheduledAtUtc = request.ScheduledAtUtc,
            DurationMinutes = request.DurationMinutes,
            MeetingLink = request.MeetingLink
        };
        await _uow.Repository<Interview>().AddAsync(interview, ct);
        await _uow.SaveChangesAsync(ct);

        var candidateName = $"{application.Candidate.User.FirstName} {application.Candidate.User.LastName}";

        await _notifications.NotifyUserAsync(
            application.Candidate.UserId, "Interview scheduled",
            $"Your {request.RoundName} interview for {application.Job.Title} is scheduled for {request.ScheduledAtUtc:g}.", ct);
        await _notifications.NotifyUserAsync(
            request.InterviewerId, "New interview assigned",
            $"You're interviewing {candidateName} for {application.Job.Title} ({request.RoundName}) at {request.ScheduledAtUtc:g}.", ct);

        await _email.SendAsync(
            application.Candidate.User.Email,
            $"Interview scheduled: {application.Job.Title}",
            $"<p>Hi {application.Candidate.User.FirstName},</p><p>Your {request.RoundName} interview has been scheduled for {request.ScheduledAtUtc:g}.</p>" +
            (request.MeetingLink is null ? "" : $"<p>Meeting link: <a href='{request.MeetingLink}'>{request.MeetingLink}</a></p>"),
            ct);

        return new InterviewDto(
            interview.Id, round.Id, round.RoundName, application.Id, application.Job.Title, candidateName,
            interviewer.Id, $"{interviewer.FirstName} {interviewer.LastName}",
            interview.ScheduledAtUtc, interview.DurationMinutes, interview.MeetingLink, interview.Result, null);
    }
}
