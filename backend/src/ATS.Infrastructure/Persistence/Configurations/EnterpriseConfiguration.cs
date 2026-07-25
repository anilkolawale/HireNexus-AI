using ATS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.Infrastructure.Persistence.Configurations;

public class JobRequisitionConfiguration : IEntityTypeConfiguration<JobRequisition>
{
    public void Configure(EntityTypeBuilder<JobRequisition> builder)
    {
        builder.Property(r => r.BudgetMin).HasColumnType("decimal(18,2)");
        builder.Property(r => r.BudgetMax).HasColumnType("decimal(18,2)");

        builder.HasOne(r => r.RequestedBy)
            .WithMany()
            .HasForeignKey(r => r.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RequisitionApprovalStepConfiguration : IEntityTypeConfiguration<RequisitionApprovalStep>
{
    public void Configure(EntityTypeBuilder<RequisitionApprovalStep> builder)
    {
        builder.HasOne(s => s.Approver)
            .WithMany()
            .HasForeignKey(s => s.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CandidateNoteConfiguration : IEntityTypeConfiguration<CandidateNote>
{
    public void Configure(EntityTypeBuilder<CandidateNote> builder)
    {
        builder.HasOne(n => n.Author)
            .WithMany()
            .HasForeignKey(n => n.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NoteMentionConfiguration : IEntityTypeConfiguration<NoteMention>
{
    public void Configure(EntityTypeBuilder<NoteMention> builder)
    {
        builder.HasOne(nm => nm.MentionedUser)
            .WithMany()
            .HasForeignKey(nm => nm.MentionedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(nm => nm.Note)
            .WithMany(n => n.Mentions)
            .HasForeignKey(nm => nm.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InterviewScorecardConfiguration : IEntityTypeConfiguration<InterviewScorecard>
{
    public void Configure(EntityTypeBuilder<InterviewScorecard> builder)
    {
        builder.HasOne(s => s.Interviewer)
            .WithMany()
            .HasForeignKey(s => s.InterviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TalentProspectConfiguration : IEntityTypeConfiguration<TalentProspect>
{
    public void Configure(EntityTypeBuilder<TalentProspect> builder)
    {
        builder.HasOne(tp => tp.AddedBy)
            .WithMany()
            .HasForeignKey(tp => tp.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ScorecardScoreConfiguration : IEntityTypeConfiguration<ScorecardScore>
{
    public void Configure(EntityTypeBuilder<ScorecardScore> builder)
    {
        builder.HasOne(s => s.Criterion)
            .WithMany(c => c.Scores)
            .HasForeignKey(s => s.CriterionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Scorecard)
            .WithMany(sc => sc.Scores)
            .HasForeignKey(s => s.ScorecardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// ── Phase 6: Industry Dominance Feature Configurations ───────────────────────

public class JobBoardPostingConfiguration : IEntityTypeConfiguration<JobBoardPosting>
{
    public void Configure(EntityTypeBuilder<JobBoardPosting> builder)
    {
        builder.Property(p => p.Board).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ExternalPostingId).HasMaxLength(500);
        builder.Property(p => p.ErrorMessage).HasMaxLength(2000);

        builder.HasOne(p => p.Job)
            .WithMany()
            .HasForeignKey(p => p.JobId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BlindScreeningConfigConfiguration : IEntityTypeConfiguration<BlindScreeningConfig>
{
    public void Configure(EntityTypeBuilder<BlindScreeningConfig> builder)
    {
        builder.HasIndex(b => b.JobId).IsUnique(); // One config per job

        builder.HasOne(b => b.Job)
            .WithMany()
            .HasForeignKey(b => b.JobId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.TriggerConfigJson).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ActionConfigJson).HasColumnType("nvarchar(max)");

        builder.HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AssessmentTemplateConfiguration : IEntityTypeConfiguration<AssessmentTemplate>
{
    public void Configure(EntityTypeBuilder<AssessmentTemplate> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Instructions).HasColumnType("nvarchar(max)");
        builder.Property(t => t.HackerRankTestId).HasMaxLength(200);

        builder.HasOne(t => t.Job)
            .WithMany()
            .HasForeignKey(t => t.JobId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VideoQuestionConfiguration : IEntityTypeConfiguration<VideoQuestion>
{
    public void Configure(EntityTypeBuilder<VideoQuestion> builder)
    {
        builder.Property(q => q.QuestionText).HasMaxLength(2000).IsRequired();

        builder.HasOne(q => q.Template)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CandidateAssessmentConfiguration : IEntityTypeConfiguration<CandidateAssessment>
{
    public void Configure(EntityTypeBuilder<CandidateAssessment> builder)
    {
        builder.Property(a => a.HackerRankInviteUrl).HasMaxLength(1000);

        builder.HasOne(a => a.Application)
            .WithMany()
            .HasForeignKey(a => a.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Template)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VideoResponseConfiguration : IEntityTypeConfiguration<VideoResponse>
{
    public void Configure(EntityTypeBuilder<VideoResponse> builder)
    {
        builder.Property(r => r.BlobVideoUrl).HasMaxLength(1000);

        builder.HasOne(r => r.Assessment)
            .WithMany(a => a.VideoResponses)
            .HasForeignKey(r => r.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Question)
            .WithMany()
            .HasForeignKey(r => r.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

