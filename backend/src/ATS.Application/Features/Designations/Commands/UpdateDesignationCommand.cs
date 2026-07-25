using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.Designations.Commands;

public record UpdateDesignationCommand(Guid Id, string Title) : IRequest<DesignationDto>;

public class UpdateDesignationCommandValidator : AbstractValidator<UpdateDesignationCommand>
{
    public UpdateDesignationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
    }
}

public class UpdateDesignationCommandHandler : IRequestHandler<UpdateDesignationCommand, DesignationDto>
{
    private readonly IUnitOfWork _uow;

    public UpdateDesignationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<DesignationDto> Handle(UpdateDesignationCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Designation>();
        var designation = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Designation), request.Id);

        designation.Title = request.Title;
        repo.Update(designation);
        await _uow.SaveChangesAsync(ct);

        return new DesignationDto(designation.Id, designation.Title, designation.DepartmentId);
    }
}
