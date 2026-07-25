using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands;

public record DeleteJobCommand(Guid JobId, Guid RequestingUserCompanyId, bool IsSuperAdmin) : IRequest;

public class DeleteJobCommandHandler : IRequestHandler<DeleteJobCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteJobCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteJobCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Job>();
        var job = await repo.Query().FirstOrDefaultAsync(j => j.Id == request.JobId, ct)
            ?? throw new NotFoundException(nameof(Job), request.JobId);

        if (!request.IsSuperAdmin && job.CompanyId != request.RequestingUserCompanyId)
            throw new ForbiddenAccessException();

        var hasApplications = await _uow.Repository<Domain.Entities.Application>()
            .ExistsAsync(a => a.JobId == request.JobId, ct);
        if (hasApplications)
            throw new ConflictException("This job has applications and cannot be deleted. Close it instead.");

        // Soft delete: preserves the row (and its audit trail) rather than a hard DELETE.
        job.IsDeleted = true;
        repo.Update(job);
        await _uow.SaveChangesAsync(ct);
    }
}
