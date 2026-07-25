using ATS.Application.Common.Models;
using ATS.Application.DTOs.Jobs;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.Jobs.Queries;

public record GetJobsQuery(
    string? SearchTerm,
    JobStatus? Status,
    string? Location,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PaginatedList<JobListItemDto>>;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, PaginatedList<JobListItemDto>>
{
    private readonly IUnitOfWork _uow;

    public GetJobsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PaginatedList<JobListItemDto>> Handle(GetJobsQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<Job>().Query().Where(j => !j.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(j => j.Title.Contains(request.SearchTerm));

        if (request.Status.HasValue)
            query = query.Where(j => j.Status == request.Status);

        if (!string.IsNullOrWhiteSpace(request.Location))
            query = query.Where(j => j.Location.Contains(request.Location));

        var projected = query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Select(j => new JobListItemDto(
                j.Id, j.Title, j.Department.Name, j.Location, j.EmploymentType,
                j.Status, j.Applications.Count, j.CreatedAtUtc));

        return await PaginatedList<JobListItemDto>.CreateAsync(projected, request.PageNumber, request.PageSize);
    }
}
