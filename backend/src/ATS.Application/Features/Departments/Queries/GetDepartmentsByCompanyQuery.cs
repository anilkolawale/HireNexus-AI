using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Departments.Queries;

public record GetDepartmentsByCompanyQuery(Guid CompanyId) : IRequest<IReadOnlyList<DepartmentDto>>;

public class GetDepartmentsByCompanyQueryHandler : IRequestHandler<GetDepartmentsByCompanyQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IUnitOfWork _uow;

    public GetDepartmentsByCompanyQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsByCompanyQuery request, CancellationToken ct)
    {
        var departments = await _uow.Repository<Department>().Query()
            .Where(d => d.CompanyId == request.CompanyId)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
        return departments.Select(d => new DepartmentDto(d.Id, d.Name, d.CompanyId)).ToList();
    }
}
