using ATS.Application.DTOs.Interviews;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Interviews.Commands;

public record SubmitFeedbackCommand(
    Guid InterviewId,
    int Rating,
    string? Strengths,
    string? Weaknesses,
    string? Comments,
    bool Recommend,
    Domain.Enums.InterviewResultStatus Result) : IRequest<FeedbackDto>;

public class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, FeedbackDto>
{
    private readonly IUnitOfWork _uow;

    public SubmitFeedbackCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<FeedbackDto> Handle(SubmitFeedbackCommand request, CancellationToken ct)
    {
        var interviewRepo = _uow.Repository<Interview>();
        var interview = await interviewRepo.Query()
            .Include(i => i.Feedback)
            .FirstOrDefaultAsync(i => i.Id == request.InterviewId, ct)
            ?? throw new NotFoundException(nameof(Interview), request.InterviewId);

        interview.Result = request.Result;
        interviewRepo.Update(interview);

        Feedback feedback;
        if (interview.Feedback is not null)
        {
            feedback = interview.Feedback;
            feedback.Rating = request.Rating;
            feedback.Strengths = request.Strengths;
            feedback.Weaknesses = request.Weaknesses;
            feedback.Comments = request.Comments;
            feedback.Recommend = request.Recommend;
            _uow.Repository<Feedback>().Update(feedback);
        }
        else
        {
            feedback = new Feedback
            {
                InterviewId = interview.Id,
                Rating = request.Rating,
                Strengths = request.Strengths,
                Weaknesses = request.Weaknesses,
                Comments = request.Comments,
                Recommend = request.Recommend
            };
            await _uow.Repository<Feedback>().AddAsync(feedback, ct);
        }

        await _uow.SaveChangesAsync(ct);

        return new FeedbackDto(feedback.Id, feedback.Rating, feedback.Strengths, feedback.Weaknesses, feedback.Comments, feedback.Recommend);
    }
}
