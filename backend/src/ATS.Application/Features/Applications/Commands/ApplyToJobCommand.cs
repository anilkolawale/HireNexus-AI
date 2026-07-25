using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Applications;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Applications.Commands;

public record ApplyToJobCommand(Guid CandidateUserId, Guid JobId) : IRequest<ApplicationDto>;

public class ApplyToJobCommandValidator : AbstractValidator<ApplyToJobCommand>
{
    public ApplyToJobCommandValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
    }
}

public class ApplyToJobCommandHandler : IRequestHandler<ApplyToJobCommand, ApplicationDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IAiService _aiService;
    private readonly INotificationService _notifications;

    public ApplyToJobCommandHandler(IUnitOfWork uow, IAiService aiService, INotificationService notifications)
    {
        _uow = uow;
        _aiService = aiService;
        _notifications = notifications;
    }

    public async Task<ApplicationDto> Handle(ApplyToJobCommand request, CancellationToken ct)
    {
        var candidate = await _uow.Repository<Candidate>().Query()
            .Include(c => c.User)
            .Include(c => c.Skills)
            .Include(c => c.Experiences)
            .FirstOrDefaultAsync(c => c.UserId == request.CandidateUserId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.CandidateUserId);

        var job = await _uow.Repository<Job>().Query()
            .Include(j => j.JobSkills)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, ct)
            ?? throw new NotFoundException(nameof(Job), request.JobId);

        var alreadyApplied = await _uow.Repository<Domain.Entities.Application>()
            .ExistsAsync(a => a.JobId == job.Id && a.CandidateId == candidate.Id, ct);
        if (alreadyApplied)
            throw new ConflictException("You have already applied to this job.");

        // AI match scoring against the job description + candidate's parsed skill/experience summary
        var resumeText = $"Skills: {string.Join(", ", candidate.Skills.Select(s => s.SkillName))}. " +
                          $"Summary: {candidate.Summary}. " +
                          $"Experience: {string.Join("; ", candidate.Experiences.Select(e => $"{e.Title} at {e.CompanyName}"))}";
        var match = await _aiService.ComputeMatchScoreAsync(resumeText, job.Description, ct);

        var application = new Domain.Entities.Application
        {
            JobId = job.Id,
            CandidateId = candidate.Id,
            Status = ApplicationStatus.Applied,
            MatchScore = match.Score,
            MissingSkillsJson = System.Text.Json.JsonSerializer.Serialize(match.MissingSkills),
            RecommendedSkillsJson = System.Text.Json.JsonSerializer.Serialize(match.RecommendedSkills),
            AiRecommendation = match.OverallRecommendation
        };

        await _uow.Repository<Domain.Entities.Application>().AddAsync(application, ct);
        await _uow.SaveChangesAsync(ct);

        await _notifications.NotifyUserAsync(
            job.CreatedByRecruiterId,
            "New application received",
            $"{candidate.User.FirstName} {candidate.User.LastName} applied to {job.Title} (match score {match.Score}).",
            ct);

        return new ApplicationDto(
            application.Id, job.Id, job.Title, candidate.Id,
            $"{candidate.User.FirstName} {candidate.User.LastName}",
            application.Status, application.MatchScore, application.CreatedAtUtc);
    }
}
