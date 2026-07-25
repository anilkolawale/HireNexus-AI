using ATS.Domain.Common;
using ATS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = ATS.Domain.Entities.Application;

namespace ATS.Infrastructure.Persistence;

public class AtsDbContext : DbContext
{
    public AtsDbContext(DbContextOptions<AtsDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<OfficeLocation> OfficeLocations => Set<OfficeLocation>();

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobSkill> JobSkills => Set<JobSkill>();

    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    // Fixed
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();

    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();

    public DbSet<InterviewRound> InterviewRounds => Set<InterviewRound>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<FileAsset> FileAssets => Set<FileAsset>();

    // ── Phase 5: Enterprise Features ────────────────────────────────────────
    public DbSet<ScorecardTemplate> ScorecardTemplates => Set<ScorecardTemplate>();
    public DbSet<ScorecardCriterion> ScorecardCriteria => Set<ScorecardCriterion>();
    public DbSet<InterviewScorecard> InterviewScorecards => Set<InterviewScorecard>();
    public DbSet<ScorecardScore> ScorecardScores => Set<ScorecardScore>();
    public DbSet<JobRequisition> JobRequisitions => Set<JobRequisition>();
    public DbSet<RequisitionApprovalStep> RequisitionApprovalSteps => Set<RequisitionApprovalStep>();
    public DbSet<CandidateNote> CandidateNotes => Set<CandidateNote>();
    public DbSet<NoteMention> NoteMentions => Set<NoteMention>();
    public DbSet<ApplicantEEOData> ApplicantEEOData => Set<ApplicantEEOData>();
    public DbSet<SlaConfig> SlaConfigs => Set<SlaConfig>();
    public DbSet<TalentProspect> TalentProspects => Set<TalentProspect>();
    public DbSet<OnboardingChecklist> OnboardingChecklists => Set<OnboardingChecklist>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();

    // ── Phase 6: Industry Dominance Features ─────────────────────────────────
    public DbSet<JobBoardPosting> JobBoardPostings => Set<JobBoardPosting>();
    public DbSet<BlindScreeningConfig> BlindScreeningConfigs => Set<BlindScreeningConfig>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<AssessmentTemplate> AssessmentTemplates => Set<AssessmentTemplate>();
    public DbSet<VideoQuestion> VideoQuestions => Set<VideoQuestion>();
    public DbSet<CandidateAssessment> CandidateAssessments => Set<CandidateAssessment>();
    public DbSet<VideoResponse> VideoResponses => Set<VideoResponse>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AtsDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AtsDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { builder });
            }
        }

        base.OnModelCreating(builder);
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder builder)
        where T : AuditableEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}