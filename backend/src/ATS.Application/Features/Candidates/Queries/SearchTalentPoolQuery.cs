using ATS.Application.Common.Models;
using ATS.Application.DTOs.Candidates;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Candidates.Queries;

// The core "search our whole talent pool" feature recruitment agencies rely on — not
// scoped to a single job's applicants. Skills search matches ANY of the comma-separated
// terms (broad recall, since recruiters usually want "React OR TypeScript OR .NET" rather
// than requiring every skill to match).
public record SearchTalentPoolQuery(
    string? SearchTerm,
    string? Skills,
    int? MinExperienceYears,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<TalentPoolRowDto>>;

public class SearchTalentPoolQueryHandler : IRequestHandler<SearchTalentPoolQuery, PaginatedList<TalentPoolRowDto>>
{
    private readonly IUnitOfWork _uow;

    public SearchTalentPoolQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedList<TalentPoolRowDto>> Handle(SearchTalentPoolQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Candidate>().Query()
            .Include(c => c.User)
            .Include(c => c.Skills)
            .Include(c => c.Applications)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c =>
                c.User.FirstName.Contains(term) ||
                c.User.LastName.Contains(term) ||
                c.User.Email.Contains(term) ||
                (c.Headline != null && c.Headline.Contains(term)) ||
                (c.CurrentEmployer != null && c.CurrentEmployer.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Skills))
        {
            var skillTerms = request.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            query = query.Where(c => c.Skills.Any(s => skillTerms.Contains(s.SkillName)));
        }

        if (request.MinExperienceYears.HasValue)
        {
            query = query.Where(c => c.Skills.Any(s => s.YearsOfExperience >= request.MinExperienceYears));
        }

        var projected = query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new TalentPoolRowDto(
                c.Id,
                c.User.FirstName + " " + c.User.LastName,
                c.User.Email,
                c.Headline,
                c.CurrentEmployer,
                c.Skills.Select(s => s.SkillName).ToList(),
                c.Applications.Count,
                c.Applications.Any(a => a.MatchScore.HasValue) ? c.Applications.Max(a => a.MatchScore) : null,
                c.CreatedAtUtc));

        return await PaginatedList<TalentPoolRowDto>.CreateAsync(projected, request.PageNumber, request.PageSize);
    }
}
