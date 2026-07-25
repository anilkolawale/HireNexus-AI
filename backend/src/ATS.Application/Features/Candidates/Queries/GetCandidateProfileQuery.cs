using ATS.Application.DTOs.Candidates;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Candidates.Queries;

public record GetCandidateProfileQuery(Guid UserId) : IRequest<CandidateProfileDto>;

public class GetCandidateProfileQueryHandler : IRequestHandler<GetCandidateProfileQuery, CandidateProfileDto>
{
    private readonly IUnitOfWork _uow;

    public GetCandidateProfileQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<CandidateProfileDto> Handle(GetCandidateProfileQuery request, CancellationToken ct)
    {
        var candidate = await _uow.Repository<Candidate>().Query()
            .Include(c => c.User)
            .Include(c => c.Skills)
            .Include(c => c.Educations)
            .Include(c => c.Experiences)
            .Include(c => c.Certificates)
            .Include(c => c.ResumeFile)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct)
            ?? throw new NotFoundException(nameof(Candidate), request.UserId);

        return new CandidateProfileDto(
            candidate.Id,
            candidate.UserId,
            $"{candidate.User.FirstName} {candidate.User.LastName}",
            candidate.User.Email,
            candidate.Headline,
            candidate.Summary,
            candidate.CurrentEmployer,
            candidate.ExpectedSalary,
            candidate.LinkedInUrl,
            candidate.PortfolioUrl,
            candidate.ResumeFile?.BlobUrl,
            candidate.Skills.Select(s => s.SkillName).ToList(),
            candidate.Educations.Select(e => new DTOs.Candidates.EducationDto(e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartYear, e.EndYear)).ToList(),
            candidate.Experiences.Select(e => new DTOs.Candidates.ExperienceDto(e.Id, e.CompanyName, e.Title, e.StartDate, e.EndDate, e.Description)).ToList(),
            candidate.Certificates.Select(c => c.Name).ToList());
    }
}
