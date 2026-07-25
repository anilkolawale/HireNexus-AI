using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.Admin.Commands;

public record SetUserActiveStatusCommand(Guid UserId, bool IsActive) : IRequest;

public class SetUserActiveStatusCommandHandler : IRequestHandler<SetUserActiveStatusCommand>
{
    private readonly IUnitOfWork _uow;

    public SetUserActiveStatusCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(SetUserActiveStatusCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<User>();
        var user = await repo.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.IsActive = request.IsActive;
        repo.Update(user);
        await _uow.SaveChangesAsync(ct);
    }
}
