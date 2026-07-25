using ATS.Application.Common.Interfaces;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Interviews.Commands;

public record SummarizeFeedbackCommand(Guid ApplicationId) : IRequest<FeedbackSummaryResult>;

internal sealed class SummarizeFeedbackCommandHandler : IRequestHandler<SummarizeFeedbackCommand, FeedbackSummaryResult>
{
    private readonly IUnitOfWork _uow;
    private readonly IAiService _ai;

    public SummarizeFeedbackCommandHandler(IUnitOfWork uow, IAiService ai)
    {
        _uow = uow;
        _ai = ai;
    }

    public async Task<FeedbackSummaryResult> Handle(SummarizeFeedbackCommand request, CancellationToken ct)
    {
        var application = await _uow.Repository<Domain.Entities.Application>()
            .Query()
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .Include(a => a.Job)
            .Include(a => a.InterviewRounds)
                .ThenInclude(r => r.Interviews)
                    .ThenInclude(i => i.Feedback)
                        .ThenInclude(f => f!.Interview).ThenInclude(i => i.Interviewer)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new KeyNotFoundException($"Application {request.ApplicationId} not found.");

        var feedbacks = application.InterviewRounds
            .SelectMany(r => r.Interviews)
            .Where(i => i.Feedback != null)
            .Select(i => new FeedbackInput(
                InterviewerName: $"{i.Interviewer.FirstName} {i.Interviewer.LastName}",
                Rating: i.Feedback!.Rating,
                Strengths: i.Feedback.Strengths,
                Weaknesses: i.Feedback.Weaknesses,
                Comments: i.Feedback.Comments,
                Recommend: i.Feedback.Recommend
            )).ToList();

        if (!feedbacks.Any())
            throw new InvalidOperationException("No feedback submitted yet for this application.");

        var candidateName = $"{application.Candidate.User.FirstName} {application.Candidate.User.LastName}";
        var result = await _ai.SummarizeFeedbackAsync(feedbacks, candidateName, application.Job.Title, ct);

        // Persist AI summary to the most recent feedback
        var latestFeedback = application.InterviewRounds
            .SelectMany(r => r.Interviews)
            .OrderByDescending(i => i.ScheduledAtUtc)
            .FirstOrDefault(i => i.Feedback != null)?.Feedback;

        if (latestFeedback != null)
        {
            latestFeedback.AiSummary = result.Summary;
            latestFeedback.AiRecommendation = result.OverallRecommendation;
            await _uow.SaveChangesAsync(ct);
        }

        return result;
    }
}
