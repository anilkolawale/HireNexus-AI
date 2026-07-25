using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.OfficeLocations.Queries;

public record GetOfficeLocationsByCompanyQuery(Guid CompanyId) : IRequest<IReadOnlyList<OfficeLocationDto>>;

public class GetOfficeLocationsByCompanyQueryHandler : IRequestHandler<GetOfficeLocationsByCompanyQuery, IReadOnlyList<OfficeLocationDto>>
{
    private readonly IUnitOfWork _uow;

    public GetOfficeLocationsByCompanyQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<OfficeLocationDto>> Handle(GetOfficeLocationsByCompanyQuery request, CancellationToken ct)
    {
        var locations = await _uow.Repository<OfficeLocation>().Query()
            .Where(l => l.CompanyId == request.CompanyId)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);
        return locations.Select(l => new OfficeLocationDto(l.Id, l.Name, l.Address, l.City, l.Country, l.CompanyId)).ToList();
    }
}
