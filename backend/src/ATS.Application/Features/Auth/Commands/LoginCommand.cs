using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Auth;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Auth.Commands;

// IpAddress/UserAgent are always set by the controller from the actual request, never bound
// from the JSON body — a client-supplied value here would be meaningless for the session list.
public record LoginCommand(string Email, string Password, string? IpAddress = null, string? UserAgent = null)
    : IRequest<AuthResultDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public LoginCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var userRepo = _uow.Repository<User>();
        var emailClean = request.Email.Trim().ToLower();
        var user = await userRepo.Query()
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == emailClean && !x.IsDeleted, ct);

        // Same generic message whether the user doesn't exist or the password is wrong,
        // so login can't be used to enumerate registered emails.
        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.LockedOutUntilUtc.HasValue && user.LockedOutUntilUtc > DateTime.UtcNow)
        {
            var minutesLeft = Math.Ceiling((user.LockedOutUntilUtc.Value - DateTime.UtcNow).TotalMinutes);
            throw new UnauthorizedAccessException($"Too many failed attempts. Try again in {minutesLeft} minute(s).");
        }

        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedOutUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }
            userRepo.Update(user);
            await _uow.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        if (!user.IsEmailVerified && user.Role?.Type == ATS.Domain.Enums.UserRoleType.Candidate)
            throw new UnauthorizedAccessException("Your email address is not verified. Please verify your email before signing in.");

        // Successful login clears any prior failed-attempt tracking.
        user.FailedLoginAttempts = 0;
        user.LockedOutUntilUtc = null;
        userRepo.Update(user);

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken(user.Id);
        refreshToken.IpAddress = request.IpAddress;
        refreshToken.UserAgent = request.UserAgent;
        await _uow.Repository<RefreshToken>().AddAsync(refreshToken, ct);

        await _uow.SaveChangesAsync(ct);

        return new AuthResultDto(
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(15),
            new UserDto(user.Id, user.FirstName, user.LastName, user.Email, user.Role?.Name ?? string.Empty, user.IsEmailVerified, user.CompanyId));
    }
}
