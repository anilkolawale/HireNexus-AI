using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Candidates.Commands;

/// <summary>
/// Reads the candidate's existing profile data, feeds it to Gemini,
/// then writes back AiProfileSummary + AiProfileScore.
/// </summary>
public record EnrichCandidateProfileCommand(Guid CandidateId) : IRequest<EnrichCandidateProfileResult>;

public record EnrichCandidateProfileResult(
    string AiProfileSummary,
    int AiProfileScore);

public class EnrichCandidateProfileCommandHandler
    : IRequestHandler<EnrichCandidateProfileCommand, EnrichCandidateProfileResult>
{
    private readonly IUnitOfWork _uow;
    private readonly IAiService _ai;

    public EnrichCandidateProfileCommandHandler(IUnitOfWork uow, IAiService ai)
    {
        _uow = uow;
        _ai  = ai;
    }

    public async Task<EnrichCandidateProfileResult> Handle(
        EnrichCandidateProfileCommand request,
        CancellationToken ct)
    {
        var candidate = await _uow.Repository<Candidate>()
            .Query()
            .Include(c => c.Skills)
            .Include(c => c.Experiences)
            .Include(c => c.Educations)
            .Include(c => c.Certificates)
            .Include(c => c.Applications)
                .ThenInclude(a => a.Job)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId, ct)
            ?? throw new KeyNotFoundException($"Candidate {request.CandidateId} not found.");

        // Build resume context from profile data
        var skillsList  = string.Join(", ", candidate.Skills.Select(s => s.SkillName));
        var experiences = string.Join("\n", candidate.Experiences
            .OrderByDescending(e => e.StartDate)
            .Select(e => $"{e.Title} at {e.CompanyName} ({e.StartDate:yyyy}–{e.EndDate?.ToString("yyyy") ?? "Present"}): {e.Description}"));
        var education   = string.Join(", ", candidate.Educations.Select(e => $"{e.Degree} in {e.FieldOfStudy} from {e.Institution}"));
        var certs       = string.Join(", ", candidate.Certificates.Select(c => c.Name));
        var latestJob   = candidate.Applications
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefault()?.Job?.Title ?? string.Empty;

        var resumeContext =
            $"Headline: {candidate.Headline}\n" +
            $"Summary: {candidate.Summary}\n" +
            $"Skills: {skillsList}\n" +
            $"Experience:\n{experiences}\n" +
            $"Education: {education}\n" +
            $"Certifications: {certs}\n" +
            $"Total experience: {candidate.YearsOfTotalExperience} years\n" +
            $"Notice period: {candidate.NoticePeriodDays} days\n" +
            $"Location preference: {candidate.LocationPreference ?? "Not specified"}\n" +
            $"Most recent role applied to: {latestJob}";

        // Generate AI summary using Gemini
        var summary = await _ai.GenerateCandidateSummaryAsync(resumeContext, ct);

        // Compute a profile completeness score (0–100)
        var score = ComputeProfileScore(candidate);

        candidate.AiProfileSummary = summary;
        candidate.AiProfileScore   = score;

        _uow.Repository<Candidate>().Update(candidate);
        await _uow.SaveChangesAsync(ct);

        return new EnrichCandidateProfileResult(summary, score);
    }

    /// <summary>
    /// Scores the candidate profile 0–100 based on field completeness.
    /// </summary>
    private static int ComputeProfileScore(Candidate c)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(c.Headline))          score += 10;
        if (!string.IsNullOrWhiteSpace(c.Summary))           score += 15;
        if (c.Skills.Any())                                   score += 20;
        if (c.Experiences.Any())                              score += 20;
        if (c.Educations.Any())                               score += 15;
        if (c.NoticePeriodDays.HasValue)                      score += 5;
        if (!string.IsNullOrWhiteSpace(c.LocationPreference)) score += 5;
        if (c.YearsOfTotalExperience.HasValue)                score += 10;
        return Math.Min(score, 100);
    }
}
