using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.OfficeLocations.Commands;

public record DeleteOfficeLocationCommand(Guid Id) : IRequest;

public class DeleteOfficeLocationCommandHandler : IRequestHandler<DeleteOfficeLocationCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteOfficeLocationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteOfficeLocationCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<OfficeLocation>();
        var location = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(OfficeLocation), request.Id);

        repo.Remove(location);
        await _uow.SaveChangesAsync(ct);
    }
}
