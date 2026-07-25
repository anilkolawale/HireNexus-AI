using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Auth;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;

namespace ATS.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwt;

    public RefreshTokenCommandHandler(IUnitOfWork uow, IJwtTokenService jwt)
    {
        _uow = uow;
        _jwt = jwt;
    }

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<RefreshToken>();
        var existing = repo.Query().FirstOrDefault(t => t.Token == request.RefreshToken);

        if (existing is null || !existing.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var userRepo = _uow.Repository<User>();
        var user = await userRepo.GetByIdAsync(existing.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        // Rotate: revoke old, issue new. Carry the original IP/UserAgent forward so this
        // remains identifiable as the same logical "session" in the session-management UI,
        // even though the underlying token value changes on every refresh.
        existing.RevokedAtUtc = DateTime.UtcNow;
        var newRefreshToken = _jwt.GenerateRefreshToken(user.Id);
        newRefreshToken.IpAddress = existing.IpAddress;
        newRefreshToken.UserAgent = existing.UserAgent;
        existing.ReplacedByToken = newRefreshToken.Token;
        repo.Update(existing);
        await repo.AddAsync(newRefreshToken, ct);

        var accessToken = _jwt.GenerateAccessToken(user);
        await _uow.SaveChangesAsync(ct);

        return new AuthResultDto(
            accessToken,
            newRefreshToken.Token,
            DateTime.UtcNow.AddMinutes(15),
            new UserDto(user.Id, user.FirstName, user.LastName, user.Email, user.Role.Name, user.IsEmailVerified, user.CompanyId));

    }
}
