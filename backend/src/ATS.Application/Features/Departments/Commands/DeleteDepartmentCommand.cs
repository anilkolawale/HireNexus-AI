using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.Departments.Commands;

public record DeleteDepartmentCommand(Guid Id) : IRequest;

public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteDepartmentCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteDepartmentCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Department>();
        var department = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        var hasJobs = await _uow.Repository<Job>().ExistsAsync(j => j.DepartmentId == request.Id, ct);
        if (hasJobs)
            throw new ConflictException("Cannot delete a department that has jobs posted. Move or close its jobs first.");

        repo.Remove(department);
        await _uow.SaveChangesAsync(ct);
    }
}
