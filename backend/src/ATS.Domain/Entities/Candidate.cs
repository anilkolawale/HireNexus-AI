using ATS.Domain.Common;

namespace ATS.Domain.Entities;

public class Candidate : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public string? Headline { get; set; }
    public string? Summary { get; set; }
    public string? CurrentEmployer { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public Guid? ResumeFileId { get; set; }
    public FileAsset? ResumeFile { get; set; }

    // AI-enriched fields
    public int? NoticePeriodDays { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public string? LocationPreference { get; set; }  // "Remote", "Hybrid", "On-site"
    public decimal? YearsOfTotalExperience { get; set; }
    public string? AiProfileSummary { get; set; }   // AI-generated recruiter summary
    public int? AiProfileScore { get; set; }         // 0-100 overall profile strength

    public ICollection<CandidateSkill> Skills { get; set; } = new List<CandidateSkill>();
    public ICollection<Education> Educations { get; set; } = new List<Education>();
    public ICollection<Experience> Experiences { get; set; } = new List<Experience>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

public class CandidateSkill : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = default!;
    public string SkillName { get; set; } = default!;
    public int? YearsOfExperience { get; set; }
    public bool ExtractedByAi { get; set; }
}

public class Education : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = default!;
    public string Institution { get; set; } = default!;
    public string Degree { get; set; } = default!;
    public string? FieldOfStudy { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
}

public class Experience : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
}

public class Certificate : BaseEntity
{
    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? IssuingOrganization { get; set; }
    public DateTime? IssueDate { get; set; }
}
