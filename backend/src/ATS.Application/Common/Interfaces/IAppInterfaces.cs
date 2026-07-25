using ATS.Domain.Entities;

namespace ATS.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? CompanyId { get; }
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string blobUrl, CancellationToken ct = default);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, string title, string message, CancellationToken ct = default);
}

// Fires outbound webhooks for company-configured subscriptions (job.published,
// application.status_changed, candidate.hired, offer.extended, etc.). Implemented in
// Infrastructure as an HTTP dispatcher with HMAC-SHA256 request signing so receivers can
// verify authenticity, and every delivery attempt is logged for the subscriber to audit.
public interface IWebhookDispatcher
{
    Task DispatchAsync(Guid companyId, string eventType, object payload, CancellationToken ct = default);
}

// AI service abstraction — implemented in ATS.AI, consumed by Application handlers
public interface IAiService
{
    Task<string> GenerateJobDescriptionAsync(string title, string department, string experienceLevel, string keySkills, CancellationToken ct = default);
    Task<ResumeParseResult> ParseResumeAsync(Stream resumeStream, string fileName, CancellationToken ct = default);
    Task<MatchScoreResult> ComputeMatchScoreAsync(string resumeText, string jobDescription, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GenerateInterviewQuestionsAsync(string jobTitle, string resumeText, string experienceLevel, CancellationToken ct = default);
    Task<string> GenerateCandidateSummaryAsync(string resumeText, CancellationToken ct = default);
    Task<string> GenerateEmailAsync(string purpose, string context, CancellationToken ct = default);
    Task<string> ChatAsync(string prompt, string contextJson, CancellationToken ct = default);

    /// <summary>Analyzes skill gaps between a candidate resume and job description.</summary>
    Task<SkillGapResult> AnalyzeSkillGapAsync(string resumeText, string jobDescription, string jobTitle, CancellationToken ct = default);

    /// <summary>Compares multiple candidates for a single job role side by side.</summary>
    Task<CandidateComparisonResult> CompareCandidatesAsync(IReadOnlyList<CandidateSummaryInput> candidates, string jobTitle, string jobDescription, CancellationToken ct = default);

    /// <summary>Summarizes all interviewer feedback into a single AI hiring recommendation.</summary>
    Task<FeedbackSummaryResult> SummarizeFeedbackAsync(IReadOnlyList<FeedbackInput> feedbacks, string candidateName, string jobTitle, CancellationToken ct = default);

    /// <summary>Drafts a professional offer letter using candidate and job data.</summary>
    Task<string> DraftOfferLetterAsync(string candidateName, string jobTitle, string companyName, decimal offeredSalary, DateTime joiningDate, CancellationToken ct = default);
}

// ── Phase 6: Industry Dominance Interfaces ────────────────────────────────────

/// <summary>
/// Generates RFC 5545 iCalendar (.ics) files and sends calendar invites via email.
/// Works without Google/Microsoft OAuth — plain .ics email attachment approach.
/// </summary>
public interface ICalendarService
{
    /// <summary>Generates an RFC 5545 VCALENDAR string for an interview.</summary>
    string GenerateIcs(Interview interview, string candidateName, string candidateEmail, string interviewerName, string interviewerEmail);

    /// <summary>Emails the .ics invite to both candidate and interviewer.</summary>
    Task SendCalendarInviteAsync(Interview interview, string candidateName, string candidateEmail, string interviewerName, string interviewerEmail, CancellationToken ct = default);
}

/// <summary>
/// Publishes job listings to external job boards (LinkedIn, Indeed, Glassdoor, ZipRecruiter).
/// Implementations are credential-gated stubs — add API keys in appsettings to activate each board.
/// </summary>
public interface IJobBoardPublisher
{
    /// <summary>Publishes a job to the specified board. Returns the external posting ID or null on failure.</summary>
    Task<string?> PublishAsync(Job job, string board, CancellationToken ct = default);

    /// <summary>Removes a job posting from the specified board.</summary>
    Task<bool> UnpublishAsync(string externalPostingId, string board, CancellationToken ct = default);

    /// <summary>Lists supported boards and their activation status (credentials configured).</summary>
    IReadOnlyList<JobBoardInfo> GetSupportedBoards();
}

/// <summary>
/// Integrates with HackerRank to send coding test invitations.
/// Config-gated — set HackerRank:ApiKey in appsettings to activate. Returns stub URL in dev.
/// </summary>
public interface IAssessmentService
{
    Task<string> CreateHackerRankInviteAsync(string candidateEmail, string testId, CancellationToken ct = default);
}

/// <summary>
/// Evaluates automation rules for a company when application events fire.
/// Called from command handlers. Implemented in Infrastructure.
/// </summary>
public interface IAutomationEngine
{
    Task EvaluateOnApplicationScoredAsync(Guid applicationId, Guid companyId, int score, CancellationToken ct = default);
    Task EvaluateOnStatusChangedAsync(Guid applicationId, Guid companyId, string toStatus, CancellationToken ct = default);
    Task EvaluateDailySlaBatchAsync(CancellationToken ct = default);
}

// ── Phase 6 result records ───────────────────────────────────────────────────

public record JobBoardInfo(string Board, string DisplayName, bool IsConfigured, string? LogoUrl);

// ── AI result records ────────────────────────────────────────────────────────

public record ResumeParseResult(
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> MissingFields,
    string Summary,
    IReadOnlyList<string> Experience,
    IReadOnlyList<string> Education,
    IReadOnlyList<string> Certifications);

public record MatchScoreResult(
    int Score,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> RecommendedSkills,
    string ExperienceFit,
    string OverallRecommendation);

public record SkillGapResult(
    IReadOnlyList<string> CandidateHas,
    IReadOnlyList<string> JobRequires,
    IReadOnlyList<string> GapSkills,
    IReadOnlyList<string> BonusSkills,
    string LearningRecommendations,
    int GapSeverity  // 1=Minor 2=Moderate 3=Critical
);

public record CandidateSummaryInput(
    string Name,
    string ResumeText,
    int MatchScore
);

public record CandidateComparisonResult(
    string BestCandidateName,
    string Summary,
    IReadOnlyList<CandidateRanking> Rankings
);

public record CandidateRanking(
    string CandidateName,
    int Rank,
    string Strengths,
    string Weaknesses,
    string HiringRecommendation
);

public record FeedbackInput(
    string InterviewerName,
    int Rating,
    string? Strengths,
    string? Weaknesses,
    string? Comments,
    bool Recommend
);

public record FeedbackSummaryResult(
    string OverallRecommendation,   // "Strong Hire" | "Hire" | "No Hire" | "Strong No Hire"
    string Summary,
    IReadOnlyList<string> KeyStrengths,
    IReadOnlyList<string> KeyConcerns,
    double AverageRating
);
