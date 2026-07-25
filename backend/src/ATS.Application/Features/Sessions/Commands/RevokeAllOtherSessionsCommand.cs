using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Sessions.Commands;

// "Log out everywhere else" — revokes every active session except the one making the request.
public record RevokeAllOtherSessionsCommand(Guid UserId, string CurrentToken) : IRequest<int>;

public class RevokeAllOtherSessionsCommandHandler : IRequestHandler<RevokeAllOtherSessionsCommand, int>
{
    private readonly IUnitOfWork _uow;

    public RevokeAllOtherSessionsCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<int> Handle(RevokeAllOtherSessionsCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<RefreshToken>();
        var otherActiveSessions = await repo.Query()
            .Where(t => t.UserId == request.UserId && t.RevokedAtUtc == null && t.Token != request.CurrentToken)
            .ToListAsync(ct);

        foreach (var token in otherActiveSessions)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            repo.Update(token);
        }

        if (otherActiveSessions.Count > 0)
            await _uow.SaveChangesAsync(ct);

        return otherActiveSessions.Count;
    }
}
