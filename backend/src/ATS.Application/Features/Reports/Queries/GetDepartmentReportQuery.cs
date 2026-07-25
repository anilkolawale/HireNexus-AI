using ATS.Application.DTOs.Reports;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Reports.Queries;

public record GetDepartmentReportQuery(Guid? CompanyId) : IRequest<ReportResult<DepartmentReportRow>>;

public class GetDepartmentReportQueryHandler : IRequestHandler<GetDepartmentReportQuery, ReportResult<DepartmentReportRow>>
{
    private readonly IUnitOfWork _uow;

    public GetDepartmentReportQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ReportResult<DepartmentReportRow>> Handle(GetDepartmentReportQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Department>().Query()
            .Include(d => d.Jobs).ThenInclude(j => j.Applications)
            .AsQueryable();

        if (request.CompanyId.HasValue) query = query.Where(d => d.CompanyId == request.CompanyId);

        var departments = await query.ToListAsync(ct);

        var rows = departments.Select(d => new DepartmentReportRow(
            d.Name,
            d.Jobs.Count(j => j.Status == JobStatus.Published),
            d.Jobs.SelectMany(j => j.Applications).Count(),
            d.Jobs.SelectMany(j => j.Applications).Count(a => a.Status == ApplicationStatus.Hired)
        )).ToList();

        return new ReportResult<DepartmentReportRow>("Department Report", rows, DateTime.UtcNow);
    }
}
