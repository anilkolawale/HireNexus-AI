using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = ATS.Domain.Exceptions.ValidationException;

namespace ATS.Application.Features.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public ResetPasswordCommandHandler(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tokenRepo = _uow.Repository<PasswordResetToken>();
        var resetToken = await tokenRepo.Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, ct);

        if (resetToken is null || !resetToken.IsActive)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["token"] = new[] { "This reset link is invalid or has expired." }
            });

        var userRepo = _uow.Repository<User>();
        resetToken.User.PasswordHash = _hasher.Hash(request.NewPassword);
        resetToken.User.FailedLoginAttempts = 0;
        resetToken.User.LockedOutUntilUtc = null;
        userRepo.Update(resetToken.User);

        resetToken.UsedAtUtc = DateTime.UtcNow;
        tokenRepo.Update(resetToken);

        // Revoking all refresh tokens forces re-login everywhere after a password reset —
        // standard practice, since the old password may have been compromised.
        var activeRefreshTokens = await _uow.Repository<RefreshToken>().Query()
            .Where(t => t.UserId == resetToken.UserId && t.RevokedAtUtc == null)
            .ToListAsync(ct);
        foreach (var rt in activeRefreshTokens)
        {
            rt.RevokedAtUtc = DateTime.UtcNow;
            _uow.Repository<RefreshToken>().Update(rt);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
