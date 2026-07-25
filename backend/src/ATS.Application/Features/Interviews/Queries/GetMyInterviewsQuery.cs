using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Interviews;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace ATS.Application.Features.Interviews.Queries;

// Interviewer's upcoming schedule across all applications.
public record GetMyInterviewsQuery(Guid InterviewerId) : IRequest<IReadOnlyList<InterviewDto>>;

public class GetMyInterviewsQueryHandler : IRequestHandler<GetMyInterviewsQuery, IReadOnlyList<InterviewDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetMyInterviewsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<InterviewDto>> Handle(GetMyInterviewsQuery request, CancellationToken ct)
    {
        var userId = request.InterviewerId;
        var role = _currentUser.Role ?? string.Empty;
        var companyId = _currentUser.CompanyId;

        var query = _uow.Repository<Interview>().Query()
            .Include(i => i.Interviewer)
            .Include(i => i.Feedback)
            .Include(i => i.InterviewRound).ThenInclude(r => r.Application).ThenInclude(a => a.Job)
            .Include(i => i.InterviewRound).ThenInclude(r => r.Application).ThenInclude(a => a.Candidate).ThenInclude(c => c.User)
            .AsQueryable();

        if (role == "Candidate")
        {
            query = query.Where(i => i.InterviewRound.Application.Candidate.UserId == userId);
        }
        // Staff roles (Recruiter, HRManager, Interviewer, SuperAdmin) see all scheduled interviews


        var interviews = await query.OrderBy(i => i.ScheduledAtUtc).ToListAsync(ct);

        return interviews.Select(i => new InterviewDto(
            i.Id, i.InterviewRoundId, i.InterviewRound.RoundName, i.InterviewRound.ApplicationId,
            i.InterviewRound.Application.Job.Title,
            $"{i.InterviewRound.Application.Candidate.User.FirstName} {i.InterviewRound.Application.Candidate.User.LastName}",
            i.InterviewerId, $"{i.Interviewer.FirstName} {i.Interviewer.LastName}",
            i.ScheduledAtUtc, i.DurationMinutes, i.MeetingLink, i.Result,
            i.Feedback == null ? null : new FeedbackDto(i.Feedback.Id, i.Feedback.Rating, i.Feedback.Strengths, i.Feedback.Weaknesses, i.Feedback.Comments, i.Feedback.Recommend)
        )).ToList();
    }
}

