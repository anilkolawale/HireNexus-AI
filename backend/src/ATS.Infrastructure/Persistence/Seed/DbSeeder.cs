using ATS.Domain.Entities;
using ATS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Persistence.Seed;

// Idempotent: safe to run on every startup. Seeds the 5 roles (required before Register
// works at all), plus a default company/department/designation/location and a SuperAdmin
// user so the app is immediately usable in dev without manual SQL.
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<ATS.Application.Common.Interfaces.IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        // The EF Core InMemory provider (used by integration tests) doesn't support
        // migrations — only migrate against a real relational provider.
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();

        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { Type = UserRoleType.SuperAdmin, Name = "SuperAdmin" },
                new Role { Type = UserRoleType.HRManager, Name = "HRManager" },
                new Role { Type = UserRoleType.Recruiter, Name = "Recruiter" },
                new Role { Type = UserRoleType.Interviewer, Name = "Interviewer" },
                new Role { Type = UserRoleType.Candidate, Name = "Candidate" }
            );
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded 5 roles.");
        }

        var superAdminRole = await context.Roles.FirstAsync(r => r.Type == UserRoleType.SuperAdmin);
        var hrManagerRole = await context.Roles.FirstAsync(r => r.Type == UserRoleType.HRManager);
        var recruiterRole = await context.Roles.FirstAsync(r => r.Type == UserRoleType.Recruiter);
        var interviewerRole = await context.Roles.FirstAsync(r => r.Type == UserRoleType.Interviewer);
        var candidateRole = await context.Roles.FirstAsync(r => r.Type == UserRoleType.Candidate);

        Company company;
        if (!await context.Companies.AnyAsync())
        {
            company = new Company { Name = "Acme Corp", Industry = "Technology", Description = "Demo company seeded for local development." };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var department = new Department { Name = "Engineering", CompanyId = company.Id };
            context.Departments.Add(department);
            await context.SaveChangesAsync();

            context.Designations.Add(new Designation { Title = "Software Engineer", DepartmentId = department.Id });
            context.OfficeLocations.Add(new OfficeLocation
            {
                Name = "HQ", Address = "123 Main St", City = "San Francisco", Country = "USA", CompanyId = company.Id
            });
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default company: {Name}", company.Name);
        }
        else
        {
            company = await context.Companies.FirstAsync();
        }

        // Seed default convenience role users
        async Task EnsureUser(string first, string last, string email, string password, Guid roleId, Guid? companyId)
        {
            if (!await context.Users.AnyAsync(u => u.Email == email))
            {
                context.Users.Add(new User
                {
                    FirstName = first,
                    LastName = last,
                    Email = email,
                    PasswordHash = passwordHasher.Hash(password),
                    RoleId = roleId,
                    CompanyId = companyId,
                    IsEmailVerified = true,
                    IsActive = true
                });
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded user: {Email}", email);
            }
        }

        await EnsureUser("Admin", "User", "admin@ats.local", "Admin@12345", superAdminRole.Id, company.Id);
        await EnsureUser("HR", "Manager", "hr@ats.local", "Admin@12345", hrManagerRole.Id, company.Id);
        await EnsureUser("Default", "Recruiter", "recruiter@ats.local", "Admin@12345", recruiterRole.Id, company.Id);
        await EnsureUser("Default", "Interviewer", "interviewer@ats.local", "Admin@12345", interviewerRole.Id, company.Id);
        await EnsureUser("Default", "Candidate", "candidate@ats.local", "Admin@12345", candidateRole.Id, null);
    }
}

