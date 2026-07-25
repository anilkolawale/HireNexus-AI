using ATS.Application.DTOs.Privacy;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Privacy.Queries;

public record ExportMyDataQuery(Guid UserId) : IRequest<DataExportDto>;

public class ExportMyDataQueryHandler : IRequestHandler<ExportMyDataQuery, DataExportDto>
{
    private readonly IUnitOfWork _uow;

    public ExportMyDataQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<DataExportDto> Handle(ExportMyDataQuery request, CancellationToken ct)
    {
        var candidate = await _uow.Repository<Candidate>().Query()
            .Include(c => c.User)
            .Include(c => c.Skills)
            .Include(c => c.Educations)
            .Include(c => c.Experiences)
            .Include(c => c.Certificates)
            .Include(c => c.Applications).ThenInclude(a => a.Job).ThenInclude(j => j.Company)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.UserId);

        var resumeFiles = await _uow.Repository<FileAsset>().Query()
            .Where(f => f.CandidateId == candidate.Id)
            .OrderByDescending(f => f.Version)
            .ToListAsync(ct);

        var profile = new DataExportProfile(
            candidate.User.FirstName,
            candidate.User.LastName,
            candidate.User.Email,
            candidate.User.PhoneNumber,
            candidate.Headline,
            candidate.Summary,
            candidate.CurrentEmployer,
            candidate.ExpectedSalary,
            candidate.LinkedInUrl,
            candidate.PortfolioUrl,
            candidate.Skills.Select(s => s.SkillName).ToList(),
            candidate.Educations.Select(e => $"{e.Degree} in {e.FieldOfStudy} — {e.Institution} ({e.StartYear}-{e.EndYear})").ToList(),
            candidate.Experiences.Select(e => $"{e.Title} at {e.CompanyName} ({e.StartDate:yyyy-MM} - {(e.EndDate.HasValue ? e.EndDate.Value.ToString("yyyy-MM") : "Present")})").ToList(),
            candidate.Certificates.Select(c => c.Name).ToList(),
            candidate.User.CreatedAtUtc);

        var applications = candidate.Applications.Select(a => new DataExportApplication(
            a.Job.Title, a.Job.Company.Name, a.Status.ToString(), a.MatchScore, a.CreatedAtUtc)).ToList();

        var resumeHistory = resumeFiles.Select(f => new DataExportResumeVersion(f.FileName, f.Version, f.UploadedAtUtc)).ToList();

        return new DataExportDto(profile, applications, resumeHistory, DateTime.UtcNow);
    }
}
