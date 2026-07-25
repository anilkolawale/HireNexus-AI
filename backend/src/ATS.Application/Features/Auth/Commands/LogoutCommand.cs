using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Auth.Commands;

// Revokes the given refresh token so it can no longer be used to mint new access tokens.
// The current access token itself remains valid until it naturally expires (15 min) since
// JWTs are stateless — this is standard and fine for this token lifetime.
public record LogoutCommand(string RefreshToken) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUnitOfWork _uow;

    public LogoutCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<RefreshToken>();
        var token = await repo.Query().FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (token is not null && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            repo.Update(token);
            await _uow.SaveChangesAsync(ct);
        }
        // Unknown/already-revoked tokens are a no-op, not an error — logout should always
        // succeed from the client's perspective even if the token was already invalidated.
    }
}
