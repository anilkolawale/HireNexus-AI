using ATS.Application.DTOs.Reports;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Reports.Queries;

public record GetHiringReportQuery(Guid? CompanyId, DateTime? FromUtc, DateTime? ToUtc) : IRequest<ReportResult<HiringReportRow>>;

public class GetHiringReportQueryHandler : IRequestHandler<GetHiringReportQuery, ReportResult<HiringReportRow>>
{
    private readonly IUnitOfWork _uow;

    public GetHiringReportQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ReportResult<HiringReportRow>> Handle(GetHiringReportQuery request, CancellationToken ct)
    {
        var jobsQuery = _uow.Repository<Job>().Query()
            .Include(j => j.Department)
            .Include(j => j.Applications).ThenInclude(a => a.StatusHistory)
            .AsQueryable();

        if (request.CompanyId.HasValue) jobsQuery = jobsQuery.Where(j => j.CompanyId == request.CompanyId);
        if (request.FromUtc.HasValue) jobsQuery = jobsQuery.Where(j => j.CreatedAtUtc >= request.FromUtc);
        if (request.ToUtc.HasValue) jobsQuery = jobsQuery.Where(j => j.CreatedAtUtc <= request.ToUtc);

        var jobs = await jobsQuery.ToListAsync(ct);

        var rows = jobs.Select(j =>
        {
            var apps = j.Applications;
            var hiredApps = apps.Where(a => a.Status == ApplicationStatus.Hired).ToList();

            var daysToHire = hiredApps
                .Select(a =>
                {
                    var hiredEvent = a.StatusHistory.Where(h => h.ToStatus == ApplicationStatus.Hired)
                        .OrderBy(h => h.ChangedAtUtc).FirstOrDefault();
                    return hiredEvent is null ? (double?)null : (hiredEvent.ChangedAtUtc - a.CreatedAtUtc).TotalDays;
                })
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            return new HiringReportRow(
                j.Title,
                j.Department.Name,
                apps.Count,
                apps.Count(a => a.Status is ApplicationStatus.Shortlisted or ApplicationStatus.TechnicalInterview or ApplicationStatus.HRInterview or ApplicationStatus.Offer or ApplicationStatus.Hired),
                apps.Count(a => a.Status is ApplicationStatus.TechnicalInterview or ApplicationStatus.HRInterview or ApplicationStatus.Offer or ApplicationStatus.Hired),
                apps.Count(a => a.Status is ApplicationStatus.Offer or ApplicationStatus.Hired),
                hiredApps.Count,
                daysToHire.Count > 0 ? daysToHire.Average() : null);
        }).ToList();

        return new ReportResult<HiringReportRow>("Hiring Report", rows, DateTime.UtcNow);
    }
}
