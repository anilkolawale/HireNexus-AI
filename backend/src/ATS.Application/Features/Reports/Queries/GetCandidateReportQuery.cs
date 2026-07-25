using ATS.Application.DTOs.Reports;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Reports.Queries;

public record GetCandidateReportQuery : IRequest<ReportResult<CandidateReportRow>>;

public class GetCandidateReportQueryHandler : IRequestHandler<GetCandidateReportQuery, ReportResult<CandidateReportRow>>
{
    private readonly IUnitOfWork _uow;

    public GetCandidateReportQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ReportResult<CandidateReportRow>> Handle(GetCandidateReportQuery request, CancellationToken ct)
    {
        var candidates = await _uow.Repository<Candidate>().Query()
            .Include(c => c.User)
            .Include(c => c.Applications)
            .Where(c => c.Applications.Any())
            .ToListAsync(ct);

        var rows = candidates.Select(c => new CandidateReportRow(
            $"{c.User.FirstName} {c.User.LastName}",
            c.User.Email,
            c.Applications.Count,
            c.Applications.Any(a => a.MatchScore.HasValue) ? c.Applications.Where(a => a.MatchScore.HasValue).Average(a => a.MatchScore!.Value) : null,
            c.Applications.OrderByDescending(a => a.CreatedAtUtc).First().Status.ToString()
        )).OrderByDescending(r => r.ApplicationsSubmitted).ToList();

        return new ReportResult<CandidateReportRow>("Candidate Report", rows, DateTime.UtcNow);
    }
}
