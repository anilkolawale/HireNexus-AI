using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.Companies.Commands;

public record CreateCompanyCommand(string Name, string? Website, string? Industry, string? Description) : IRequest<CompanyDto>;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    private readonly IUnitOfWork _uow;

    public CreateCompanyCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<CompanyDto> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        var company = new Company
        {
            Name = request.Name,
            Website = request.Website,
            Industry = request.Industry,
            Description = request.Description
        };
        await _uow.Repository<Company>().AddAsync(company, ct);
        await _uow.SaveChangesAsync(ct);

        return new CompanyDto(company.Id, company.Name, company.Website, company.LogoUrl, company.Industry, company.Description);
    }
}
