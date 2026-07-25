using ATS.Domain.Common;
using ATS.Domain.Enums;

namespace ATS.Domain.Entities;

// ── FEATURE 2: JOB BOARD MULTI-POSTING ───────────────────────────────────────

/// <summary>
/// Tracks the posting status of a job on an external job board (LinkedIn, Indeed, etc.)
/// One record per job per board. Updated by the background publisher service.
/// </summary>
public class JobBoardPosting : AuditableEntity
{
    public Guid JobId { get; set; }
    public Job Job { get; set; } = default!;

    public string Board { get; set; } = default!; // "LinkedIn" | "Indeed" | "Glassdoor" | "ZipRecruiter"
    public string? ExternalPostingId { get; set; }
    public JobBoardPostingStatus Status { get; set; } = JobBoardPostingStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

// ── FEATURE 3: BLIND SCREENING ───────────────────────────────────────────────

/// <summary>
/// Controls which candidate PII fields are masked during recruiter review for a specific job.
/// Designed to reduce unconscious bias in the screening process (EEOC-aligned best practice).
/// </summary>
public class BlindScreeningConfig : AuditableEntity
{
    public Guid JobId { get; set; }
    public Job Job { get; set; } = default!;

    public bool IsEnabled { get; set; } = false;
    public bool HideName { get; set; } = true;
    public bool HidePhoto { get; set; } = true;
    public bool HideGender { get; set; } = false;
    public bool HideEthnicity { get; set; } = false;
    public bool HideAge { get; set; } = false;
}

// ── FEATURE 4: WORKFLOW AUTOMATION ENGINE ────────────────────────────────────

/// <summary>
/// A configurable trigger-action automation rule for a company.
/// Examples: "When match score > 85 → send screening email", "When SLA > 3 days → notify recruiter".
/// Evaluated by AutomationEngine (Hangfire + MediatR pipeline behaviors).
/// </summary>
public class AutomationRule : AuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;

    public AutomationTrigger Trigger { get; set; }
    public string TriggerConfigJson { get; set; } = "{}";
    // MatchScoreAbove:       {"minScore": 85}
    // ApplicationStatusChanged: {"toStatus": "Shortlisted"}
    // DaysInStageExceeds:    {"days": 3, "stage": "Applied"}

    public AutomationAction Action { get; set; }
    public string ActionConfigJson { get; set; } = "{}";
    // SendEmail:             {"subject": "...", "body": "..."}
    // SendNotification:      {"message": "..."}
    // MoveToStage:           {"stage": "Shortlisted"}

    public int ExecutionCount { get; set; } = 0;
    public DateTime? LastFiredAtUtc { get; set; }
}

// ── FEATURE 5: VIDEO INTERVIEW & ASSESSMENT ───────────────────────────────────

/// <summary>
/// A reusable assessment template for a job, containing video questions and/or a HackerRank coding test.
/// Created by recruiters/HRManagers.
/// </summary>
public class AssessmentTemplate : AuditableEntity
{
    public Guid JobId { get; set; }
    public Job Job { get; set; } = default!;

    public string Title { get; set; } = default!;
    public AssessmentType Type { get; set; } = AssessmentType.Video;
    public int DurationMinutes { get; set; } = 30;
    public string? Instructions { get; set; }

    // HackerRank coding test ID (null if Type is Video only)
    public string? HackerRankTestId { get; set; }

    public ICollection<VideoQuestion> Questions { get; set; } = new List<VideoQuestion>();
    public ICollection<CandidateAssessment> Assignments { get; set; } = new List<CandidateAssessment>();
}

/// <summary>A single async video question within an assessment template.</summary>
public class VideoQuestion : BaseEntity
{
    public Guid TemplateId { get; set; }
    public AssessmentTemplate Template { get; set; } = default!;

    public string QuestionText { get; set; } = default!;
    public int ThinkTimeSecs { get; set; } = 30;      // Prep time before recording starts
    public int RecordingTimeSecs { get; set; } = 120;  // Max recording duration
    public int Order { get; set; } = 1;
}

/// <summary>
/// An assessment instance assigned to a specific candidate application.
/// Tracks completion status and links to HackerRank invite URL if applicable.
/// </summary>
public class CandidateAssessment : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;

    public Guid TemplateId { get; set; }
    public AssessmentTemplate Template { get; set; } = default!;

    public AssessmentStatus Status { get; set; } = AssessmentStatus.Pending;
    public DateTime? SentAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    // Set when HackerRank coding test is assigned
    public string? HackerRankInviteUrl { get; set; }

    public ICollection<VideoResponse> VideoResponses { get; set; } = new List<VideoResponse>();
}

/// <summary>
/// A candidate's recorded video response to a single assessment question.
/// The video is stored as a blob (Azure Storage / Azurite dev).
/// </summary>
public class VideoResponse : BaseEntity
{
    public Guid AssessmentId { get; set; }
    public CandidateAssessment Assessment { get; set; } = default!;

    public Guid QuestionId { get; set; }
    public VideoQuestion Question { get; set; } = default!;

    public string? BlobVideoUrl { get; set; }      // Azure Blob URL of recorded video
    public int? DurationSeconds { get; set; }      // Actual recording duration
    public DateTime? SubmittedAtUtc { get; set; }
}
