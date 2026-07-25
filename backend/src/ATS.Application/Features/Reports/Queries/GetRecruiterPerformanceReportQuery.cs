using ATS.Application.DTOs.Reports;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Reports.Queries;

public record GetRecruiterPerformanceReportQuery(Guid? CompanyId) : IRequest<ReportResult<RecruiterPerformanceRow>>;

public class GetRecruiterPerformanceReportQueryHandler : IRequestHandler<GetRecruiterPerformanceReportQuery, ReportResult<RecruiterPerformanceRow>>
{
    private readonly IUnitOfWork _uow;

    public GetRecruiterPerformanceReportQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ReportResult<RecruiterPerformanceRow>> Handle(GetRecruiterPerformanceReportQuery request, CancellationToken ct)
    {
        var jobsQuery = _uow.Repository<Job>().Query()
            .Include(j => j.CreatedByRecruiter)
            .Include(j => j.Applications)
            .AsQueryable();

        if (request.CompanyId.HasValue) jobsQuery = jobsQuery.Where(j => j.CompanyId == request.CompanyId);

        var jobs = await jobsQuery.ToListAsync(ct);
        var interviews = await _uow.Repository<Interview>().Query()
            .Include(i => i.InterviewRound).ThenInclude(r => r.Application).ThenInclude(a => a.Job)
            .ToListAsync(ct);
        var offers = await _uow.Repository<Offer>().Query()
            .Include(o => o.Application).ThenInclude(a => a.Job)
            .ToListAsync(ct);

        var rows = jobs
            .GroupBy(j => new { j.CreatedByRecruiterId, Name = $"{j.CreatedByRecruiter.FirstName} {j.CreatedByRecruiter.LastName}" })
            .Select(g =>
            {
                var jobIds = g.Select(j => j.Id).ToHashSet();
                var apps = g.SelectMany(j => j.Applications).ToList();

                return new RecruiterPerformanceRow(
                    g.Key.Name,
                    g.Count(),
                    apps.Count,
                    interviews.Count(i => jobIds.Contains(i.InterviewRound.Application.JobId)),
                    offers.Count(o => jobIds.Contains(o.Application.JobId)),
                    apps.Count(a => a.Status == ApplicationStatus.Hired));
            })
            .OrderByDescending(r => r.Hires)
            .ToList();

        return new ReportResult<RecruiterPerformanceRow>("Recruiter Performance Report", rows, DateTime.UtcNow);
    }
}
