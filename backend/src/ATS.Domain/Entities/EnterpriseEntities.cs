using ATS.Domain.Common;
using ATS.Domain.Enums;

namespace ATS.Domain.Entities;

// ── SCORECARDS ────────────────────────────────────────────────────────────────

/// <summary>Defines evaluation criteria for interviews on a specific job.</summary>
public class ScorecardTemplate : AuditableEntity
{
    public Guid JobId { get; set; }
    public Job Job { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? AiGeneratedCriteria { get; set; }  // Raw JSON from Gemini

    public ICollection<ScorecardCriterion> Criteria { get; set; } = new List<ScorecardCriterion>();
    public ICollection<InterviewScorecard> Scorecards { get; set; } = new List<InterviewScorecard>();
}

/// <summary>A single evaluation criterion within a scorecard template.</summary>
public class ScorecardCriterion : BaseEntity
{
    public Guid TemplateId { get; set; }
    public ScorecardTemplate Template { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int Weight { get; set; } = 20;  // percentage 1–100
    public int Order { get; set; }

    public ICollection<ScorecardScore> Scores { get; set; } = new List<ScorecardScore>();
}

/// <summary>An interviewer's completed evaluation for a specific interview.</summary>
public class InterviewScorecard : AuditableEntity
{
    public Guid TemplateId { get; set; }
    public ScorecardTemplate Template { get; set; } = default!;
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = default!;
    public Guid InterviewerId { get; set; }
    public User Interviewer { get; set; } = default!;
    public string? OverallComment { get; set; }
    public ScorecardDecision? Decision { get; set; }
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }

    public ICollection<ScorecardScore> Scores { get; set; } = new List<ScorecardScore>();
}

/// <summary>A rating on a specific criterion within an interviewer's scorecard.</summary>
public class ScorecardScore : BaseEntity
{
    public Guid ScorecardId { get; set; }
    public InterviewScorecard Scorecard { get; set; } = default!;
    public Guid CriterionId { get; set; }
    public ScorecardCriterion Criterion { get; set; } = default!;
    public int Rating { get; set; }  // 1–5
    public string? Comment { get; set; }
}

// ── JOB REQUISITION APPROVAL ──────────────────────────────────────────────────

/// <summary>
/// A headcount request that must pass through an approval chain before a job can be created.
/// Mandatory for enterprises — Finance/Manager must approve before recruiting begins.
/// </summary>
public class JobRequisition : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Department { get; set; }
    public string? Description { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public int HeadcountRequested { get; set; } = 1;
    public RequisitionStatus Status { get; set; } = RequisitionStatus.Draft;
    public Guid? LinkedJobId { get; set; }           // Set once approved and job created
    public string? RejectionReason { get; set; }

    public ICollection<RequisitionApprovalStep> ApprovalSteps { get; set; } = new List<RequisitionApprovalStep>();
}

/// <summary>One step in the multi-level approval chain for a job requisition.</summary>
public class RequisitionApprovalStep : AuditableEntity
{
    public Guid RequisitionId { get; set; }
    public JobRequisition Requisition { get; set; } = default!;
    public Guid ApproverId { get; set; }
    public User Approver { get; set; } = default!;
    public ApprovalStepStatus Status { get; set; } = ApprovalStepStatus.Pending;
    public string StepName { get; set; } = default!;  // "Manager", "HR Director", "Finance"
    public int StepOrder { get; set; }
    public string? Comment { get; set; }
    public DateTime? ActedAtUtc { get; set; }
}

// ── CANDIDATE NOTES & COLLABORATION ──────────────────────────────────────────

/// <summary>
/// A structured note left by any hiring team member on a candidate's application.
/// Replaces email chains — central collaboration hub per candidate.
/// </summary>
public class CandidateNote : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = default!;
    public string Content { get; set; } = default!;
    public NoteVisibility Visibility { get; set; } = NoteVisibility.Public;
    public bool IsPinned { get; set; }

    public ICollection<NoteMention> Mentions { get; set; } = new List<NoteMention>();
}

/// <summary>An @mention of a user in a candidate note — triggers a real-time notification.</summary>
public class NoteMention : BaseEntity
{
    public Guid NoteId { get; set; }
    public CandidateNote Note { get; set; } = default!;
    public Guid MentionedUserId { get; set; }
    public User MentionedUser { get; set; } = default!;
}

// ── EEO / DIVERSITY DATA ──────────────────────────────────────────────────────

/// <summary>
/// Voluntary EEO demographic data collected at application time.
/// Required for EEOC, OFCCP, and ESG reporting. Stored separately from application data for GDPR.
/// </summary>
public class ApplicantEEOData : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    public EEOGender? Gender { get; set; }
    public EEOEthnicity? Ethnicity { get; set; }
    public bool? IsVeteran { get; set; }
    public bool? HasDisability { get; set; }
}

// ── SLA CONFIGURATION ─────────────────────────────────────────────────────────

/// <summary>
/// Per-stage SLA limits for a company. Hangfire checks hourly and alerts when breached.
/// </summary>
public class SlaConfig : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public string StageName { get; set; } = default!;   // matches ApplicationStatus name
    public int MaxDays { get; set; } = 3;
    public bool IsActive { get; set; } = true;
}

// ── TALENT PROSPECT CRM ───────────────────────────────────────────────────────

/// <summary>
/// A passive/potential candidate in the talent CRM pipeline (Lever-style TRM).
/// Tracked before they ever apply — keeps a warm talent pool.
/// </summary>
public class TalentProspect : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public Guid? AddedByUserId { get; set; }
    public User? AddedBy { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? CurrentTitle { get; set; }
    public string? Skills { get; set; }
    public string? Notes { get; set; }
    public ProspectSource Source { get; set; } = ProspectSource.Other;
    public ProspectStatus Status { get; set; } = ProspectStatus.New;
    public string? AiOutreachEmail { get; set; }        // Gemini-drafted outreach
    public DateTime? LastContactedAtUtc { get; set; }
}

// ── ONBOARDING MODULE ─────────────────────────────────────────────────────────

/// <summary>
/// An onboarding checklist auto-created when a candidate is Hired.
/// Tracks IT setup, document signing, HR orientation tasks etc.
/// </summary>
public class OnboardingChecklist : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    public DateTime? StartDate { get; set; }
    public OnboardingStatus Status { get; set; } = OnboardingStatus.NotStarted;

    public ICollection<OnboardingTask> Tasks { get; set; } = new List<OnboardingTask>();
}

/// <summary>A single task within an onboarding checklist.</summary>
public class OnboardingTask : AuditableEntity
{
    public Guid ChecklistId { get; set; }
    public OnboardingChecklist Checklist { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string AssignedTo { get; set; } = "HR";     // HR, IT, Manager, NewHire
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int Order { get; set; }
}
