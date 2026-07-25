using ATS.Application.DTOs.Applications;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Applications.Queries;

public record GetMyApplicationsQuery(Guid CandidateUserId) : IRequest<IReadOnlyList<ApplicationDetailDto>>;

public class GetMyApplicationsQueryHandler : IRequestHandler<GetMyApplicationsQuery, IReadOnlyList<ApplicationDetailDto>>
{
    private readonly IUnitOfWork _uow;

    public GetMyApplicationsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<ApplicationDetailDto>> Handle(GetMyApplicationsQuery request, CancellationToken ct)
    {
        var applications = await _uow.Repository<Domain.Entities.Application>().Query()
            .Include(a => a.Job)
            .Include(a => a.Candidate)
            .Where(a => a.Candidate.UserId == request.CandidateUserId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(ct);

        return applications.Select(a => new ApplicationDetailDto(
            a.Id, a.JobId, a.Job.Title,
            $"{a.Candidate?.User?.FirstName} {a.Candidate?.User?.LastName}".Trim(),
            a.Status, a.MatchScore,
            Deserialize(a.MissingSkillsJson), Deserialize(a.RecommendedSkillsJson),
            a.AiRecommendation, a.CreatedAtUtc)).ToList();

    }

    private static List<string> Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
}
