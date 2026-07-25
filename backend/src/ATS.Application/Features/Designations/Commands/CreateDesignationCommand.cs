using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.Designations.Commands;

public record CreateDesignationCommand(string Title, Guid DepartmentId) : IRequest<DesignationDto>;

public class CreateDesignationCommandValidator : AbstractValidator<CreateDesignationCommand>
{
    public CreateDesignationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}

public class CreateDesignationCommandHandler : IRequestHandler<CreateDesignationCommand, DesignationDto>
{
    private readonly IUnitOfWork _uow;

    public CreateDesignationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<DesignationDto> Handle(CreateDesignationCommand request, CancellationToken ct)
    {
        var designation = new Designation { Title = request.Title, DepartmentId = request.DepartmentId };
        await _uow.Repository<Designation>().AddAsync(designation, ct);
        await _uow.SaveChangesAsync(ct);
        return new DesignationDto(designation.Id, designation.Title, designation.DepartmentId);
    }
}
