using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.Departments.Commands;

public record UpdateDepartmentCommand(Guid Id, string Name) : IRequest<DepartmentDto>;

public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    private readonly IUnitOfWork _uow;

    public UpdateDepartmentCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Department>();
        var department = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        department.Name = request.Name;
        repo.Update(department);
        await _uow.SaveChangesAsync(ct);

        return new DepartmentDto(department.Id, department.Name, department.CompanyId);
    }
}
