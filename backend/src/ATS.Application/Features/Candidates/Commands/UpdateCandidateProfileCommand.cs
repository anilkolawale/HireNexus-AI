using ATS.Application.DTOs.Candidates;
using ATS.Domain.Entities;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Candidates.Commands;

public record UpdateCandidateProfileCommand(
    Guid UserId,
    string? Headline,
    string? Summary,
    string? CurrentEmployer,
    decimal? ExpectedSalary,
    string? LinkedInUrl,
    string? PortfolioUrl) : IRequest<CandidateProfileDto>;

public class UpdateCandidateProfileCommandValidator : AbstractValidator<UpdateCandidateProfileCommand>
{
    public UpdateCandidateProfileCommandValidator()
    {
        RuleFor(x => x.Headline).MaximumLength(200);
        RuleFor(x => x.ExpectedSalary).GreaterThanOrEqualTo(0).When(x => x.ExpectedSalary.HasValue);
    }
}

// Creates the Candidate row on first save (a User with role Candidate may not have one yet).
public class UpdateCandidateProfileCommandHandler : IRequestHandler<UpdateCandidateProfileCommand, CandidateProfileDto>
{
    private readonly IUnitOfWork _uow;

    public UpdateCandidateProfileCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<CandidateProfileDto> Handle(UpdateCandidateProfileCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Candidate>();
        var candidate = await repo.Query().Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        var isNewCandidate = candidate is null;
        if (candidate is null)
        {
            var user = await _uow.Repository<User>().GetByIdAsync(request.UserId, ct)
                ?? throw new NotFoundException(nameof(User), request.UserId);
            candidate = new Candidate { UserId = request.UserId, User = user };
            await repo.AddAsync(candidate, ct);
        }

        candidate.Headline = request.Headline ?? candidate.Headline;
        candidate.Summary = request.Summary ?? candidate.Summary;
        candidate.CurrentEmployer = request.CurrentEmployer ?? candidate.CurrentEmployer;
        candidate.ExpectedSalary = request.ExpectedSalary ?? candidate.ExpectedSalary;
        candidate.LinkedInUrl = request.LinkedInUrl ?? candidate.LinkedInUrl;
        candidate.PortfolioUrl = request.PortfolioUrl ?? candidate.PortfolioUrl;

        if (!isNewCandidate)
        {
            repo.Update(candidate);
        }
        await _uow.SaveChangesAsync(ct);


        return new CandidateProfileDto(
            candidate.Id, candidate.UserId, $"{candidate.User.FirstName} {candidate.User.LastName}",
            candidate.User.Email, candidate.Headline, candidate.Summary, candidate.CurrentEmployer,
            candidate.ExpectedSalary, candidate.LinkedInUrl, candidate.PortfolioUrl, null,
            new List<string>(), new List<DTOs.Candidates.EducationDto>(), new List<DTOs.Candidates.ExperienceDto>(), new List<string>());
    }
}
