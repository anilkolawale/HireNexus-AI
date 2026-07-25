namespace ATS.Domain.Enums;

public enum UserRoleType
{
    SuperAdmin = 1,
    HRManager = 2,
    Recruiter = 3,
    Interviewer = 4,
    Candidate = 5
}

public enum EmploymentType
{
    FullTime = 1,
    PartTime = 2,
    Contract = 3,
    Internship = 4,
    Remote = 5
}

public enum JobStatus
{
    Draft = 1,
    Published = 2,
    Closed = 3
}

public enum ApplicationStatus
{
    Applied = 1,
    Screening = 2,
    Shortlisted = 3,
    TechnicalInterview = 4,
    HRInterview = 5,
    Offer = 6,
    Hired = 7,
    Rejected = 8
}

public enum InterviewResultStatus
{
    Pending = 1,
    Passed = 2,
    Failed = 3,
    NoShow = 4
}

public enum NotificationType
{
    ApplicationStatusChanged = 1,
    InterviewScheduled = 2,
    InterviewReminder = 3,
    OfferExtended = 4,
    General = 5
}

// ── Phase 5: Enterprise Features ────────────────────────────────────────────

public enum ScorecardDecision
{
    StrongHire = 1,
    Hire = 2,
    NoHire = 3,
    StrongNoHire = 4
}

public enum NoteVisibility
{
    Public = 1,
    HiringManagerOnly = 2,
    Private = 3
}

public enum RequisitionStatus
{
    Draft = 1,
    PendingManagerApproval = 2,
    PendingHRApproval = 3,
    PendingFinanceApproval = 4,
    Approved = 5,
    Rejected = 6
}

public enum ApprovalStepStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum ProspectSource
{
    LinkedIn = 1,
    Referral = 2,
    CareerSite = 3,
    Agency = 4,
    ColdOutreach = 5,
    JobBoard = 6,
    Other = 7
}

public enum ProspectStatus
{
    New = 1,
    Contacted = 2,
    Interested = 3,
    NotInterested = 4,
    Converted = 5
}

public enum OnboardingStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3
}

public enum EEOGender
{
    Male = 1,
    Female = 2,
    NonBinary = 3,
    PreferNotToSay = 4
}

public enum EEOEthnicity
{
    HispanicLatino = 1,
    WhiteNotHispanic = 2,
    BlackAfricanAmerican = 3,
    NativeHawaiianPacificIslander = 4,
    AsianAmericanPacificIslander = 5,
    AmericanIndianAlaskaNative = 6,
    TwoOrMoreRaces = 7,
    PreferNotToSay = 8
}

// ── Phase 6: Industry Dominance Features ─────────────────────────────────────

public enum JobBoardPostingStatus
{
    Pending = 1,
    Active = 2,
    Failed = 3,
    Closed = 4
}

public enum AutomationTrigger
{
    MatchScoreAbove = 1,
    ApplicationStatusChanged = 2,
    DaysInStageExceeds = 3,
    ApplicationReceived = 4,
    CandidateHired = 5
}

public enum AutomationAction
{
    SendEmail = 1,
    SendNotification = 2,
    MoveToStage = 3,
    AssignToRecruiter = 4,
    CreateTask = 5
}

public enum AssessmentType
{
    Video = 1,
    CodingTest = 2,
    Mixed = 3
}

public enum AssessmentStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Expired = 4
}
