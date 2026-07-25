using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.Companies.Commands;

public record UpdateCompanyCommand(Guid Id, string Name, string? Website, string? Industry, string? Description) : IRequest<CompanyDto>;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, CompanyDto>
{
    private readonly IUnitOfWork _uow;

    public UpdateCompanyCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<CompanyDto> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Company>();
        var company = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Company), request.Id);

        company.Name = request.Name;
        company.Website = request.Website;
        company.Industry = request.Industry;
        company.Description = request.Description;

        repo.Update(company);
        await _uow.SaveChangesAsync(ct);

        return new CompanyDto(company.Id, company.Name, company.Website, company.LogoUrl, company.Industry, company.Description);
    }
}
