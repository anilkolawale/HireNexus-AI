using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Sessions.Commands;

public record RevokeSessionCommand(Guid SessionId, Guid UserId) : IRequest;

public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand>
{
    private readonly IUnitOfWork _uow;

    public RevokeSessionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(RevokeSessionCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<RefreshToken>();
        var token = await repo.Query()
            .FirstOrDefaultAsync(t => t.Id == request.SessionId, ct)
            ?? throw new NotFoundException(nameof(RefreshToken), request.SessionId);

        if (token.UserId != request.UserId)
            throw new ForbiddenAccessException();

        if (token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            repo.Update(token);
            await _uow.SaveChangesAsync(ct);
        }
    }
}
