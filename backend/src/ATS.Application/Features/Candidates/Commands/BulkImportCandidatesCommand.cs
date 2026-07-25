using ATS.Application.Common.Interfaces;
using ATS.Application.DTOs.Candidates;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Candidates.Commands;

// Expects a raw CSV string with header: FirstName,LastName,Email,Skills
// Skills is a semicolon-separated list, e.g. "React;TypeScript;.NET"
// Imported candidates get a random temporary password — they'd normally reset it via the
// "forgot password" flow before first login, since bulk-imported profiles don't come with
// a known password the candidate chose themselves.
public record BulkImportCandidatesCommand(string CsvContent) : IRequest<BulkImportResultDto>;

public class BulkImportCandidatesCommandHandler : IRequestHandler<BulkImportCandidatesCommand, BulkImportResultDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public BulkImportCandidatesCommandHandler(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    public async Task<BulkImportResultDto> Handle(BulkImportCandidatesCommand request, CancellationToken ct)
    {
        var lines = request.CsvContent
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (lines.Count == 0)
            return new BulkImportResultDto(0, 0, 0, new List<BulkImportRowResultDto>());

        // Skip header row
        var dataRows = lines.Skip(1).ToList();
        var results = new List<BulkImportRowResultDto>();

        var candidateRole = _uow.Repository<Role>().Query().FirstOrDefault(r => r.Type == UserRoleType.Candidate)
            ?? throw new Domain.Exceptions.NotFoundException(nameof(Role), UserRoleType.Candidate);

        for (var i = 0; i < dataRows.Count; i++)
        {
            var rowNumber = i + 2; // account for header row + 1-indexing
            var columns = dataRows[i].Split(',').Select(c => c.Trim()).ToArray();

            if (columns.Length < 3)
            {
                results.Add(new BulkImportRowResultDto(rowNumber, "", false, "Expected at least FirstName,LastName,Email"));
                continue;
            }

            var (firstName, lastName, email) = (columns[0], columns[1], columns[2]);
            var skills = columns.Length > 3 ? columns[3].Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() : new List<string>();

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                results.Add(new BulkImportRowResultDto(rowNumber, email, false, "Invalid email"));
                continue;
            }

            var exists = await _uow.Repository<User>().ExistsAsync(u => u.Email == email, ct);
            if (exists)
            {
                results.Add(new BulkImportRowResultDto(rowNumber, email, false, "A user with this email already exists"));
                continue;
            }

            try
            {
                var tempPassword = $"Temp@{Guid.NewGuid():N}"[..16];
                var user = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PasswordHash = _hasher.Hash(tempPassword),
                    RoleId = candidateRole.Id,
                    IsEmailVerified = false,
                    IsActive = true
                };
                await _uow.Repository<User>().AddAsync(user, ct);

                var candidate = new Candidate { UserId = user.Id, User = user };
                await _uow.Repository<Candidate>().AddAsync(candidate, ct);

                foreach (var skill in skills)
                {
                    await _uow.Repository<CandidateSkill>().AddAsync(new CandidateSkill
                    {
                        CandidateId = candidate.Id,
                        SkillName = skill,
                        ExtractedByAi = false
                    }, ct);
                }

                results.Add(new BulkImportRowResultDto(rowNumber, email, true, null));
            }
            catch (Exception ex)
            {
                results.Add(new BulkImportRowResultDto(rowNumber, email, false, ex.Message));
            }
        }

        await _uow.SaveChangesAsync(ct);

        return new BulkImportResultDto(dataRows.Count, results.Count(r => r.Success), results.Count(r => !r.Success), results);
    }
}
