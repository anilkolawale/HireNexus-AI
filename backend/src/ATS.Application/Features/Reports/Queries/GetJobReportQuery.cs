using ATS.Application.DTOs.Reports;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Reports.Queries;

public record GetJobReportQuery(Guid? CompanyId) : IRequest<ReportResult<JobReportRow>>;

public class GetJobReportQueryHandler : IRequestHandler<GetJobReportQuery, ReportResult<JobReportRow>>
{
    private readonly IUnitOfWork _uow;

    public GetJobReportQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ReportResult<JobReportRow>> Handle(GetJobReportQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Job>().Query().Include(j => j.Applications).AsQueryable();
        if (request.CompanyId.HasValue) query = query.Where(j => j.CompanyId == request.CompanyId);

        var jobs = await query.OrderByDescending(j => j.CreatedAtUtc).ToListAsync(ct);

        var rows = jobs.Select(j => new JobReportRow(
            j.Title, j.Status.ToString(), j.CreatedAtUtc,
            j.Applications.Count, j.Applications.Count(a => a.Status == ApplicationStatus.Hired)
        )).ToList();

        return new ReportResult<JobReportRow>("Job Report", rows, DateTime.UtcNow);
    }
}
