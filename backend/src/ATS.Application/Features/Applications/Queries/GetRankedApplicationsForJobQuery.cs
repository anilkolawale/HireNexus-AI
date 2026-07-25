using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Applications;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Applications.Queries;

// Recruiter-facing: candidates for a job, ranked by AI match score (highest first).
public record GetRankedApplicationsForJobQuery(Guid JobId) : IRequest<IReadOnlyList<ApplicationDto>>;

public class GetRankedApplicationsForJobQueryHandler : IRequestHandler<GetRankedApplicationsForJobQuery, IReadOnlyList<ApplicationDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetRankedApplicationsForJobQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ApplicationDto>> Handle(GetRankedApplicationsForJobQuery request, CancellationToken ct)
    {
        var job = await _uow.Repository<Job>().Query()
            .FirstOrDefaultAsync(j => j.Id == request.JobId, ct)
            ?? throw new NotFoundException(nameof(Job), request.JobId);

        // Return ranked applications for the specified job


        var applications = await _uow.Repository<Domain.Entities.Application>().Query()
            .Include(a => a.Job)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .Where(a => a.JobId == request.JobId)
            .OrderByDescending(a => a.MatchScore)
            .ToListAsync(ct);

        return applications.Select(a => new ApplicationDto(
            a.Id, a.JobId, a.Job.Title, a.CandidateId,
            $"{a.Candidate.User.FirstName} {a.Candidate.User.LastName}",
            a.Status, a.MatchScore, a.CreatedAtUtc)).ToList();
    }
}
