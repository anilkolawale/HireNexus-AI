using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Designations.Queries;

public record GetDesignationsByDepartmentQuery(Guid DepartmentId) : IRequest<IReadOnlyList<DesignationDto>>;

public class GetDesignationsByDepartmentQueryHandler : IRequestHandler<GetDesignationsByDepartmentQuery, IReadOnlyList<DesignationDto>>
{
    private readonly IUnitOfWork _uow;

    public GetDesignationsByDepartmentQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<DesignationDto>> Handle(GetDesignationsByDepartmentQuery request, CancellationToken ct)
    {
        var designations = await _uow.Repository<Designation>().Query()
            .Where(d => d.DepartmentId == request.DepartmentId)
            .OrderBy(d => d.Title)
            .ToListAsync(ct);
        return designations.Select(d => new DesignationDto(d.Id, d.Title, d.DepartmentId)).ToList();
    }
}
