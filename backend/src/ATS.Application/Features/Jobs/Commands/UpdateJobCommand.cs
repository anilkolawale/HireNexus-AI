using ATS.Application.DTOs.Jobs;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands;

public record UpdateJobCommand(
    Guid JobId,
    Guid RequestingUserCompanyId,
    bool IsSuperAdmin,
    string Title,
    string Description,
    string? Responsibilities,
    string? Benefits,
    string ExperienceRequired,
    decimal SalaryMin,
    decimal SalaryMax,
    string Location,
    EmploymentType EmploymentType,
    List<string> Skills) : IRequest<JobDto>;

public class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(x => x.SalaryMin);
        RuleFor(x => x.Skills).NotEmpty();
    }
}

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, JobDto>
{
    private readonly IUnitOfWork _uow;

    public UpdateJobCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<JobDto> Handle(UpdateJobCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Job>();
        var job = await repo.Query()
            .Include(j => j.JobSkills)
            .Include(j => j.Department)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, ct)
            ?? throw new NotFoundException(nameof(Job), request.JobId);

        if (!request.IsSuperAdmin && job.CompanyId != request.RequestingUserCompanyId)
            throw new ForbiddenAccessException();

        job.Title = request.Title;
        job.Description = request.Description;
        job.Responsibilities = request.Responsibilities;
        job.Benefits = request.Benefits;
        job.ExperienceRequired = request.ExperienceRequired;
        job.SalaryMin = request.SalaryMin;
        job.SalaryMax = request.SalaryMax;
        job.Location = request.Location;
        job.EmploymentType = request.EmploymentType;

        var skillRepo = _uow.Repository<JobSkill>();
        foreach (var existing in job.JobSkills.ToList())
            skillRepo.Remove(existing);
        foreach (var skillName in request.Skills.Distinct())
            await skillRepo.AddAsync(new JobSkill { JobId = job.Id, SkillName = skillName }, ct);

        repo.Update(job);
        await _uow.SaveChangesAsync(ct);

        return new JobDto(
            job.Id, job.Title, job.Description, job.Department.Name, job.ExperienceRequired,
            job.SalaryMin, job.SalaryMax, job.Location, job.EmploymentType, job.Status,
            request.Skills, job.CreatedAtUtc);
    }
}
