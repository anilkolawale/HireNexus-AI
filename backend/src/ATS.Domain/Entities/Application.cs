using ATS.Domain.Common;
using ATS.Domain.Enums;

namespace ATS.Domain.Entities;

public class Application : AuditableEntity
{
    public Guid JobId { get; set; }
    public Job Job { get; set; } = default!;

    public Guid CandidateId { get; set; }
    public Candidate Candidate { get; set; } = default!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

    // AI-generated match data
    public int? MatchScore { get; set; }
    public string? MissingSkillsJson { get; set; }
    public string? RecommendedSkillsJson { get; set; }
    public string? AiRecommendation { get; set; }

    public ICollection<InterviewRound> InterviewRounds { get; set; } = new List<InterviewRound>();
    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
    public Offer? Offer { get; set; }
}

public class ApplicationStatusHistory : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    public ApplicationStatus FromStatus { get; set; }
    public ApplicationStatus ToStatus { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid ChangedByUserId { get; set; }
    public string? Notes { get; set; }
}
