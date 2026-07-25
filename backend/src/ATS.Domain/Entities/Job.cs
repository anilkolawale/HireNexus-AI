using ATS.Domain.Common;
using ATS.Domain.Enums;

namespace ATS.Domain.Entities;

public class Job : AuditableEntity
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? Responsibilities { get; set; }
    public string? Benefits { get; set; }
    public string ExperienceRequired { get; set; } = default!;
    public decimal SalaryMin { get; set; }
    public decimal SalaryMax { get; set; }
    public string Location { get; set; } = default!;
    public EmploymentType EmploymentType { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Draft;

    public DateTime? ClosingDate { get; set; }
    public bool RemoteOption { get; set; } = false;
    public int TotalPositions { get; set; } = 1;
    public string? AiGeneratedDescription { get; set; }  // AI draft stored separately

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public Guid HiringManagerId { get; set; }
    public User HiringManager { get; set; } = default!;

    public Guid CreatedByRecruiterId { get; set; }
    public User CreatedByRecruiter { get; set; } = default!;

    public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

public class JobSkill : BaseEntity
{
    public Guid JobId { get; set; }
    public Job Job { get; set; } = default!;
    public string SkillName { get; set; } = default!;
    public bool IsRequired { get; set; } = true;
}
