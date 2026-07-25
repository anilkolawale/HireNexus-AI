using ATS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.Infrastructure.Persistence.Configurations;

public class InterviewRoundConfiguration : IEntityTypeConfiguration<InterviewRound>
{
    public void Configure(EntityTypeBuilder<InterviewRound> builder)
    {
        builder.HasOne(r => r.Application).WithMany(a => a.InterviewRounds)
            .HasForeignKey(r => r.ApplicationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.HasOne(i => i.InterviewRound).WithMany(r => r.Interviews)
            .HasForeignKey(i => i.InterviewRoundId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Interviewer).WithMany()
            .HasForeignKey(i => i.InterviewerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Feedback).WithOne(f => f.Interview)
            .HasForeignKey<Feedback>(f => f.InterviewId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.Property(o => o.OfferedSalary).HasColumnType("decimal(18,2)");
        builder.HasOne(o => o.Application).WithOne(a => a.Offer)
            .HasForeignKey<Offer>(o => o.ApplicationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.HasOne(h => h.Application).WithMany(a => a.StatusHistory)
            .HasForeignKey(h => h.ApplicationId).OnDelete(DeleteBehavior.Cascade);
    }
}
