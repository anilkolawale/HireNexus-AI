using ATS.Application.DTOs.Jobs;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ATS.Application.Features.Jobs.Commands;

public record CreateJobCommand(
    string Title,
    string Description,
    string? Responsibilities,
    string? Benefits,
    Guid DepartmentId,
    Guid CompanyId,
    Guid HiringManagerId,
    Guid CreatedByRecruiterId,
    string ExperienceRequired,
    decimal SalaryMin,
    decimal SalaryMax,
    string Location,
    EmploymentType EmploymentType,
    List<string> Skills) : IRequest<JobDto>;

public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.SalaryMin).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalaryMax).GreaterThanOrEqualTo(x => x.SalaryMin)
            .WithMessage("SalaryMax must be greater than or equal to SalaryMin.");
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.Skills).NotEmpty().WithMessage("At least one skill is required.");
    }
}

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobDto>
{
    private readonly IUnitOfWork _uow;

    public CreateJobCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<JobDto> Handle(CreateJobCommand request, CancellationToken ct)
    {
        var job = new Job
        {
            Title = request.Title,
            Description = request.Description,
            Responsibilities = request.Responsibilities,
            Benefits = request.Benefits,
            DepartmentId = request.DepartmentId,
            CompanyId = request.CompanyId,
            HiringManagerId = request.HiringManagerId,
            CreatedByRecruiterId = request.CreatedByRecruiterId,
            ExperienceRequired = request.ExperienceRequired,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Location = request.Location,
            EmploymentType = request.EmploymentType,
            Status = JobStatus.Draft,
            JobSkills = request.Skills.Select(s => new JobSkill { SkillName = s }).ToList()
        };

        await _uow.Repository<Job>().AddAsync(job, ct);
        await _uow.SaveChangesAsync(ct);

        return new JobDto(
            job.Id, job.Title, job.Description, request.DepartmentId.ToString(),
            job.ExperienceRequired, job.SalaryMin, job.SalaryMax, job.Location,
            job.EmploymentType, job.Status,
            job.JobSkills.Select(s => s.SkillName).ToList(),
            job.CreatedAtUtc);
    }
}
