using ATS.Domain.Common;
using ATS.Domain.Enums;

namespace ATS.Domain.Entities;

public class InterviewRound : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    public string RoundName { get; set; } = default!; // e.g. "Technical", "HR"
    public int SequenceOrder { get; set; }
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}

public class Interview : AuditableEntity
{
    public Guid InterviewRoundId { get; set; }
    public InterviewRound InterviewRound { get; set; } = default!;

    public Guid InterviewerId { get; set; }
    public User Interviewer { get; set; } = default!;

    public DateTime ScheduledAtUtc { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string? MeetingLink { get; set; }
    public InterviewResultStatus Result { get; set; } = InterviewResultStatus.Pending;
    public DateTime? ReminderSentAtUtc { get; set; }  // Set when 24h reminder is dispatched

    // Phase 6: Calendar Integration
    public string? CalendarEventId { get; set; }  // External calendar event ID (Google/Outlook)
    public string? IcsFileUrl { get; set; }        // Blob URL of stored .ics file

    public Feedback? Feedback { get; set; }
}

public class Feedback : AuditableEntity
{
    public Guid InterviewId { get; set; }
    public Interview Interview { get; set; } = default!;
    public int Rating { get; set; } // 1-5
    public string? Strengths { get; set; }
    public string? Weaknesses { get; set; }
    public string? Comments { get; set; }
    public bool Recommend { get; set; }
    public string? AiSummary { get; set; }          // AI-generated summary of all feedback
    public string? AiRecommendation { get; set; }   // AI hiring recommendation
}

public class Offer : AuditableEntity
{
    public Guid ApplicationId { get; set; }
    public Application Application { get; set; } = default!;
    public decimal OfferedSalary { get; set; }
    public DateTime JoiningDate { get; set; }
    public string? Notes { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public string? DraftLetterText { get; set; }    // AI-drafted offer letter
}
