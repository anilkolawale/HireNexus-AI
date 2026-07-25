using ATS.Application.DTOs.Interviews;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Interviews.Queries;

public record GetInterviewsForApplicationQuery(Guid ApplicationId) : IRequest<IReadOnlyList<InterviewRoundDto>>;

public class GetInterviewsForApplicationQueryHandler : IRequestHandler<GetInterviewsForApplicationQuery, IReadOnlyList<InterviewRoundDto>>
{
    private readonly IUnitOfWork _uow;

    public GetInterviewsForApplicationQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<InterviewRoundDto>> Handle(GetInterviewsForApplicationQuery request, CancellationToken ct)
    {
        var rounds = await _uow.Repository<InterviewRound>().Query()
            .Include(r => r.Interviews).ThenInclude(i => i.Interviewer)
            .Include(r => r.Interviews).ThenInclude(i => i.Feedback)
            .Include(r => r.Application).ThenInclude(a => a.Job)
            .Include(r => r.Application).ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .Where(r => r.ApplicationId == request.ApplicationId)
            .OrderBy(r => r.SequenceOrder)
            .ToListAsync(ct);

        return rounds.Select(r => new InterviewRoundDto(
            r.Id, r.RoundName, r.SequenceOrder,
            r.Interviews.Select(i => new InterviewDto(
                i.Id, r.Id, r.RoundName, r.ApplicationId, r.Application.Job.Title,
                $"{r.Application.Candidate.User.FirstName} {r.Application.Candidate.User.LastName}",
                i.InterviewerId, $"{i.Interviewer.FirstName} {i.Interviewer.LastName}",
                i.ScheduledAtUtc, i.DurationMinutes, i.MeetingLink, i.Result,
                i.Feedback == null ? null : new FeedbackDto(i.Feedback.Id, i.Feedback.Rating, i.Feedback.Strengths, i.Feedback.Weaknesses, i.Feedback.Comments, i.Feedback.Recommend)
            )).ToList())).ToList();
    }
}
