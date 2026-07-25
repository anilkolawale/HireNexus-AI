using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Auth;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace ATS.Application.Features.Auth.Commands;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    UserRoleType Role,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<AuthResultDto>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResultDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public RegisterCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenService jwt, IEmailService email, IConfiguration config)
    {
        _uow = uow;
        _hasher = hasher;
        _jwt = jwt;
        _email = email;
        _config = config;
    }

    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        var userRepo = _uow.Repository<User>();
        var exists = await userRepo.ExistsAsync(u => u.Email == request.Email, ct);
        if (exists)
            throw new ConflictException("A user with this email already exists.");

        var roleRepo = _uow.Repository<Role>();
        var role = roleRepo.Query().FirstOrDefault(r => r.Type == request.Role)
            ?? throw new NotFoundException(nameof(Role), request.Role);

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            RoleId = role.Id,
            Role = role,
            IsEmailVerified = false,
            IsActive = true
        };

        await userRepo.AddAsync(user, ct);

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken(user.Id);
        refreshToken.IpAddress = request.IpAddress;
        refreshToken.UserAgent = request.UserAgent;
        await _uow.Repository<RefreshToken>().AddAsync(refreshToken, ct);

        var verificationToken = System.Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        await _uow.Repository<EmailVerificationToken>().AddAsync(new EmailVerificationToken
        {
            UserId = user.Id,
            Token = verificationToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        }, ct);

        await _uow.SaveChangesAsync(ct);

        var verifyLink = $"{_config["App:FrontendBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173"}/verify-email?token={verificationToken}";
        await _email.SendAsync(
            user.Email,
            "Welcome — verify your email",
            $"<p>Hi {user.FirstName},</p><p>Welcome to the ATS. Please verify your email address:</p>" +
            $"<p><a href='{verifyLink}'>Verify Email</a></p>",
            ct);

        return new AuthResultDto(
            accessToken,
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(15),
            new UserDto(user.Id, user.FirstName, user.LastName, user.Email, role.Name, user.IsEmailVerified, user.CompanyId));

    }
}
