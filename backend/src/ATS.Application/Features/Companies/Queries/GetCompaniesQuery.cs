using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Companies.Queries;

public record GetCompaniesQuery : IRequest<IReadOnlyList<CompanyDto>>;

public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    private readonly IUnitOfWork _uow;

    public GetCompaniesQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken ct)
    {
        var companies = await _uow.Repository<Company>().Query().OrderBy(c => c.Name).ToListAsync(ct);
        return companies.Select(c => new CompanyDto(c.Id, c.Name, c.Website, c.LogoUrl, c.Industry, c.Description)).ToList();
    }
}
