using ATS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.Infrastructure.Persistence.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.HasOne(c => c.User).WithMany()
            .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.UserId).IsUnique();

        builder.HasOne(c => c.ResumeFile).WithMany()
            .HasForeignKey(c => c.ResumeFileId).OnDelete(DeleteBehavior.SetNull);

        builder.Property(c => c.ExpectedSalary).HasColumnType("decimal(18,2)");
        builder.Property(c => c.YearsOfTotalExperience).HasColumnType("decimal(5,1)");

        builder.HasMany(c => c.Skills).WithOne(s => s.Candidate)
            .HasForeignKey(s => s.CandidateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Educations).WithOne(e => e.Candidate)
            .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Experiences).WithOne(e => e.Candidate)
            .HasForeignKey(e => e.CandidateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Certificates).WithOne(cert => cert.Candidate)
            .HasForeignKey(cert => cert.CandidateId).OnDelete(DeleteBehavior.Cascade);
    }
}
