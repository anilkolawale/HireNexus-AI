using ATS.Application.DTOs.Jobs;
using ATS.Domain.Entities;
using ATS.Domain.Enums;
using ATS.Domain.Exceptions;
using ATS.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands;

public record DuplicateJobCommand(Guid JobId, Guid RequestingUserCompanyId, bool IsSuperAdmin, Guid RequestingUserId) : IRequest<JobDto>;

public class DuplicateJobCommandHandler : IRequestHandler<DuplicateJobCommand, JobDto>
{
    private readonly IUnitOfWork _uow;

    public DuplicateJobCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<JobDto> Handle(DuplicateJobCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<Job>();
        var source = await repo.Query()
            .Include(j => j.JobSkills)
            .Include(j => j.Department)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, ct)
            ?? throw new NotFoundException(nameof(Job), request.JobId);

        if (!request.IsSuperAdmin && source.CompanyId != request.RequestingUserCompanyId)
            throw new ForbiddenAccessException();

        var copy = new Job
        {
            Title = $"{source.Title} (Copy)",
            Description = source.Description,
            Responsibilities = source.Responsibilities,
            Benefits = source.Benefits,
            DepartmentId = source.DepartmentId,
            CompanyId = source.CompanyId,
            HiringManagerId = source.HiringManagerId,
            CreatedByRecruiterId = request.RequestingUserId,
            ExperienceRequired = source.ExperienceRequired,
            SalaryMin = source.SalaryMin,
            SalaryMax = source.SalaryMax,
            Location = source.Location,
            EmploymentType = source.EmploymentType,
            Status = JobStatus.Draft,
            JobSkills = source.JobSkills.Select(s => new JobSkill { SkillName = s.SkillName }).ToList()
        };

        await repo.AddAsync(copy, ct);
        await _uow.SaveChangesAsync(ct);

        return new JobDto(
            copy.Id, copy.Title, copy.Description, source.Department.Name, copy.ExperienceRequired,
            copy.SalaryMin, copy.SalaryMax, copy.Location, copy.EmploymentType, copy.Status,
            copy.JobSkills.Select(s => s.SkillName).ToList(), copy.CreatedAtUtc);
    }
}
