using System.Security.Cryptography;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace ATS.Application.Features.Auth.Commands;

public record ResendVerificationEmailCommand(Guid UserId) : IRequest;

public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;

    public ResendVerificationEmailCommandHandler(IUnitOfWork uow, IEmailService email, IConfiguration config)
    {
        _uow = uow;
        _email = email;
        _config = config;
    }

    public async Task Handle(ResendVerificationEmailCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.IsEmailVerified)
            return;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        await _uow.Repository<EmailVerificationToken>().AddAsync(new EmailVerificationToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        }, ct);
        await _uow.SaveChangesAsync(ct);

        var verifyLink = $"{_config["App:FrontendBaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173"}/verify-email?token={token}";
        await _email.SendAsync(
            user.Email,
            "Verify your email",
            $"<p>Hi {user.FirstName},</p><p>Click below to verify your email address.</p>" +
            $"<p><a href='{verifyLink}'>Verify Email</a></p>",
            ct);
    }
}
