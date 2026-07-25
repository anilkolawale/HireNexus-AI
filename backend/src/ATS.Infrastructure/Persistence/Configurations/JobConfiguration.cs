using ATS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.Property(j => j.Title).IsRequired().HasMaxLength(200);
        builder.Property(j => j.Description).IsRequired();
        builder.Property(j => j.SalaryMin).HasColumnType("decimal(18,2)");
        builder.Property(j => j.SalaryMax).HasColumnType("decimal(18,2)");

        builder.HasOne(j => j.Company).WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.Department).WithMany(d => d.Jobs)
            .HasForeignKey(j => j.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.HiringManager).WithMany()
            .HasForeignKey(j => j.HiringManagerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.CreatedByRecruiter).WithMany()
            .HasForeignKey(j => j.CreatedByRecruiterId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(j => j.JobSkills).WithOne(s => s.Job)
            .HasForeignKey(s => s.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationConfiguration : IEntityTypeConfiguration<ATS.Domain.Entities.Application>
{
    public void Configure(EntityTypeBuilder<ATS.Domain.Entities.Application> builder)
    {
        builder.HasOne(a => a.Job).WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Candidate).WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.JobId, a.CandidateId }).IsUnique();
    }
}
