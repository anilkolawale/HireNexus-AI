using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Applications;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Applications.Queries;

public record GetAllPipelineApplicationsQuery : IRequest<IReadOnlyList<ApplicationDetailDto>>;

public class GetAllPipelineApplicationsQueryHandler : IRequestHandler<GetAllPipelineApplicationsQuery, IReadOnlyList<ApplicationDetailDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetAllPipelineApplicationsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ApplicationDetailDto>> Handle(GetAllPipelineApplicationsQuery request, CancellationToken ct)
    {
        var role = _currentUser.Role ?? string.Empty;
        var userId = _currentUser.UserId;

        var query = _uow.Repository<Domain.Entities.Application>().Query()
            .Include(a => a.Job)
            .Include(a => a.Candidate).ThenInclude(c => c.User)
            .AsQueryable();

        if (role == "Candidate")
        {
            query = query.Where(a => a.Candidate.UserId == userId);
        }

        var applications = await query
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
