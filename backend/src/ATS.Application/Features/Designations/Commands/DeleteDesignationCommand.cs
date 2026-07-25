using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.Designations.Commands;

public record DeleteDesignationCommand(Guid Id) : IRequest;

public class DeleteDesignationCommandHandler : IRequestHandler<DeleteDesignationCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteDesignationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteDesignationCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Designation>();
        var designation = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Designation), request.Id);

        repo.Remove(designation);
        await _uow.SaveChangesAsync(ct);
    }
}
