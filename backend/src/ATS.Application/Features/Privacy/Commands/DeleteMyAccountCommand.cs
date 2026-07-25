using ATS.Application.Common.Interfaces;
using ATS.Domain.Entities;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NotFoundException = ATS.Domain.Exceptions.NotFoundException;
using ValidationException = ATS.Domain.Exceptions.ValidationException;

namespace ATS.Application.Features.Privacy.Commands;

// GDPR Article 17 (right to erasure), implemented as anonymization rather than a hard delete:
// Applications/Interviews/Offers reference the candidate and are legitimately retained for
// audit/compliance periods by employers (equal-opportunity records, tax records for anyone
// hired, etc.) — deleting the row outright would break that history and referential integrity.
// Instead: PII is scrubbed beyond recovery, the account is deactivated and soft-deleted, all
// sessions are revoked, and the resume file is removed from Blob Storage. This is a reasonable
// starting point, not a substitute for legal review of your specific retention obligations.
public record DeleteMyAccountCommand(Guid UserId, string ConfirmationPhrase) : IRequest;

public class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand>
{
    private const string RequiredPhrase = "DELETE MY ACCOUNT";

    private readonly IUnitOfWork _uow;
    private readonly IBlobStorageService _blobStorage;

    public DeleteMyAccountCommandHandler(IUnitOfWork uow, IBlobStorageService blobStorage)
    {
        _uow = uow;
        _blobStorage = blobStorage;
    }

    public async Task Handle(DeleteMyAccountCommand request, CancellationToken ct)
    {
        if (!string.Equals(request.ConfirmationPhrase?.Trim(), RequiredPhrase, StringComparison.Ordinal))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["confirmationPhrase"] = new[] { $"You must type \"{RequiredPhrase}\" exactly to confirm." }
            });

        var userRepo = _uow.Repository<User>();
        var user = await userRepo.Query()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var anonymizedTag = Guid.NewGuid().ToString("N")[..12];
        user.FirstName = "Deleted";
        user.LastName = "User";
        user.Email = $"deleted-{anonymizedTag}@erased.local";
        user.PhoneNumber = null;
        user.ProfileImageUrl = null;
        user.PasswordHash = Guid.NewGuid().ToString(); // unusable — this account can never log in again
        user.IsActive = false;
        user.IsDeleted = true;
        userRepo.Update(user);

        var candidate = await _uow.Repository<Candidate>().Query()
            .Include(c => c.ResumeFile)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (candidate is not null)
        {
            candidate.Headline = null;
            candidate.Summary = null;
            candidate.CurrentEmployer = null;
            candidate.LinkedInUrl = null;
            candidate.PortfolioUrl = null;
            candidate.ExpectedSalary = null;

            if (candidate.ResumeFile is not null)
            {
                try { await _blobStorage.DeleteAsync(candidate.ResumeFile.BlobUrl, ct); } catch { /* best effort */ }
            }

            _uow.Repository<Candidate>().Update(candidate);
        }

        var activeTokens = await _uow.Repository<RefreshToken>().Query()
            .Where(t => t.UserId == request.UserId && t.RevokedAtUtc == null)
            .ToListAsync(ct);
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            _uow.Repository<RefreshToken>().Update(token);
        }

        await _uow.SaveChangesAsync(ct);
    }
}
