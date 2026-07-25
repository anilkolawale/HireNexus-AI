using ATS.Application.Common.Interfaces;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Applications.Queries;

public record CompareCandidatesQuery(IReadOnlyList<Guid> ApplicationIds, Guid JobId) : IRequest<CandidateComparisonResult>;

internal sealed class CompareCandidatesQueryHandler : IRequestHandler<CompareCandidatesQuery, CandidateComparisonResult>
{
    private readonly IUnitOfWork _uow;
    private readonly IAiService _ai;

    public CompareCandidatesQueryHandler(IUnitOfWork uow, IAiService ai)
    {
        _uow = uow;
        _ai = ai;
    }

    public async Task<CandidateComparisonResult> Handle(CompareCandidatesQuery request, CancellationToken ct)
    {
        var job = await _uow.Repository<Domain.Entities.Job>()
            .Query()
            .FirstOrDefaultAsync(j => j.Id == request.JobId, ct)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found.");

        var applications = await _uow.Repository<Domain.Entities.Application>()
            .Query()
            .Include(a => a.Candidate)
                .ThenInclude(c => c.User)
            .Include(a => a.Candidate)
                .ThenInclude(c => c.Skills)
            .Where(a => request.ApplicationIds.Contains(a.Id))
            .ToListAsync(ct);

        var inputs = applications.Select(a => new CandidateSummaryInput(
            Name: $"{a.Candidate.User.FirstName} {a.Candidate.User.LastName}",
            ResumeText: a.Candidate.AiProfileSummary ?? string.Join(", ", a.Candidate.Skills.Select(s => s.SkillName)),
            MatchScore: a.MatchScore ?? 0
        )).ToList();

        return await _ai.CompareCandidatesAsync(inputs, job.Title, job.Description, ct);
    }
}
