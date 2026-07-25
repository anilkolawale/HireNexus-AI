using System.Security.Cryptography;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ATS.Application.Features.Auth.Commands;

// Always "succeeds" from the caller's perspective (no response body indicating whether the
// email exists) to avoid leaking which emails are registered. The reset link/email is only
// actually sent if a matching, active user is found.
public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public ForgotPasswordCommandHandler(IUnitOfWork uow, IEmailService email, IConfiguration config)
    {
        _uow = uow;
        _email = email;
        _config = config;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().Query()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive && !u.IsDeleted, ct);

        if (user is null)
            return; // Deliberately silent — see class remarks.

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        await _uow.Repository<PasswordResetToken>().AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        }, ct);
        await _uow.SaveChangesAsync(ct);

        var resetLink = $"{_config["App:FrontendBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173"}/reset-password?token={token}";
        await _email.SendAsync(
            user.Email,
            "Reset your ATS password",
            $"<p>Hi {user.FirstName},</p><p>Click below to reset your password. This link expires in 1 hour.</p>" +
            $"<p><a href='{resetLink}'>Reset Password</a></p><p>If you didn't request this, you can ignore this email.</p>",
            ct);
    }
}
