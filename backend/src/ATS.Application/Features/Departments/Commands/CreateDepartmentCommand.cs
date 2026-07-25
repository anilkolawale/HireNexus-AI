using ATS.Application.DTOs.Companies;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.Departments.Commands;

public record CreateDepartmentCommand(string Name, Guid CompanyId) : IRequest<DepartmentDto>;

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IUnitOfWork _uow;

    public CreateDepartmentCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken ct)
    {
        var department = new Department { Name = request.Name, CompanyId = request.CompanyId };
        await _uow.Repository<Department>().AddAsync(department, ct);
        await _uow.SaveChangesAsync(ct);
        return new DepartmentDto(department.Id, department.Name, department.CompanyId);
    }
}
