using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.OfficeLocations.Commands;

public record UpdateOfficeLocationCommand(Guid Id, string Name, string Address, string City, string Country) : IRequest<OfficeLocationDto>;

public class UpdateOfficeLocationCommandValidator : AbstractValidator<UpdateOfficeLocationCommand>
{
    public UpdateOfficeLocationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
    }
}

public class UpdateOfficeLocationCommandHandler : IRequestHandler<UpdateOfficeLocationCommand, OfficeLocationDto>
{
    private readonly IUnitOfWork _uow;

    public UpdateOfficeLocationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<OfficeLocationDto> Handle(UpdateOfficeLocationCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<OfficeLocation>();
        var location = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(OfficeLocation), request.Id);

        location.Name = request.Name;
        location.Address = request.Address;
        location.City = request.City;
        location.Country = request.Country;

        repo.Update(location);
        await _uow.SaveChangesAsync(ct);

        return new OfficeLocationDto(location.Id, location.Name, location.Address, location.City, location.Country, location.CompanyId);
    }
}
