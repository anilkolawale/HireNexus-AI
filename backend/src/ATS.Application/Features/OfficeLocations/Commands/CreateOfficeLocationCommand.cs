using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.OfficeLocations.Commands;

public record CreateOfficeLocationCommand(string Name, string Address, string City, string Country, Guid CompanyId)
    : IRequest<OfficeLocationDto>;

public class CreateOfficeLocationCommandValidator : AbstractValidator<CreateOfficeLocationCommand>
{
    public CreateOfficeLocationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class CreateOfficeLocationCommandHandler : IRequestHandler<CreateOfficeLocationCommand, OfficeLocationDto>
{
    private readonly IUnitOfWork _uow;

    public CreateOfficeLocationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<OfficeLocationDto> Handle(CreateOfficeLocationCommand request, CancellationToken ct)
    {
        var location = new OfficeLocation
        {
            Name = request.Name,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            CompanyId = request.CompanyId
        };
        await _uow.Repository<OfficeLocation>().AddAsync(location, ct);
        await _uow.SaveChangesAsync(ct);
        return new OfficeLocationDto(location.Id, location.Name, location.Address, location.City, location.Country, location.CompanyId);
    }
}
