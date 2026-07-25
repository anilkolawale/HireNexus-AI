using ATS.Domain.Entities;
using ValidationException = ATS.Domain.Exceptions.ValidationException;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Auth.Commands;

public record VerifyEmailCommand(string Token) : IRequest;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IUnitOfWork _uow;

    public VerifyEmailCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var tokenRepo = _uow.Repository<EmailVerificationToken>();
        var token = await tokenRepo.Query()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, ct);

        if (token is null || !token.IsActive)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["token"] = new[] { "This verification link is invalid or has expired." }
            });

        token.User.IsEmailVerified = true;
        _uow.Repository<User>().Update(token.User);

        token.VerifiedAtUtc = DateTime.UtcNow;
        tokenRepo.Update(token);

        await _uow.SaveChangesAsync(ct);
    }
}
