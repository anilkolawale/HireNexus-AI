using ATS.Domain.Entities;
using ATS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ATS.Infrastructure.Persistence.Seed;

// Separate from DbSeeder (which only seeds the minimum required to run: roles + one
// SuperAdmin). This seeds a full realistic dataset — companies, departments, jobs (published,
// so they're actually visible on the job board), recruiters/interviewers, candidates with
// skills and resumes, applications with AI match scores, interviews with feedback, and an
// offer — so the app is demoable and clickable immediately after first run. Gated behind
// Seed:IncludeDemoData in appsettings so it's a one-line change to turn off for a real
// production deployment, where you'd want DbSeeder's minimal bootstrap only.
public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (!config.GetValue("Seed:IncludeDemoData", true))
            return;

        var context = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<ATS.Application.Common.Interfaces.IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

        // Idempotency guard: if we've already seeded demo user hr@acme-demo.local, demo data has already run — don't duplicate it.
        if (await context.Users.AnyAsync(u => u.Email == "hr@acme-demo.local"))
            return;

        logger.LogInformation("Seeding demo data...");

        var roles = await context.Roles.ToDictionaryAsync(r => r.Type);
        var acme = await context.Companies.FirstAsync(c => c.Name == "Acme Corp");
        var acmeEngineering = await context.Departments.FirstOrDefaultAsync(d => d.CompanyId == acme.Id && d.Name == "Engineering");
        if (acmeEngineering is null)
        {
            acmeEngineering = new Department { Id = Guid.NewGuid(), Name = "Engineering", CompanyId = acme.Id };
            context.Departments.Add(acmeEngineering);
            await context.SaveChangesAsync();
        }

        // --- Additional departments/designations/locations for Acme ---
        var acmeProduct = await context.Departments.FirstOrDefaultAsync(d => d.CompanyId == acme.Id && d.Name == "Product");
        if (acmeProduct is null)
        {
            acmeProduct = new Department { Id = Guid.NewGuid(), Name = "Product", CompanyId = acme.Id };
            context.Departments.Add(acmeProduct);
        }

        var acmeSales = await context.Departments.FirstOrDefaultAsync(d => d.CompanyId == acme.Id && d.Name == "Sales");
        if (acmeSales is null)
        {
            acmeSales = new Department { Id = Guid.NewGuid(), Name = "Sales", CompanyId = acme.Id };
            context.Departments.Add(acmeSales);
        }
        await context.SaveChangesAsync();

        if (!await context.Designations.AnyAsync(d => d.DepartmentId == acmeEngineering.Id))
        {
            context.Designations.Add(new Designation { Id = Guid.NewGuid(), Title = "Senior Software Engineer", DepartmentId = acmeEngineering.Id });
        }
        if (!await context.Designations.AnyAsync(d => d.DepartmentId == acmeProduct.Id))
        {
            context.Designations.Add(new Designation { Id = Guid.NewGuid(), Title = "Product Manager", DepartmentId = acmeProduct.Id });
        }
        if (!await context.Designations.AnyAsync(d => d.DepartmentId == acmeSales.Id))
        {
            context.Designations.Add(new Designation { Id = Guid.NewGuid(), Title = "Account Executive", DepartmentId = acmeSales.Id });
        }

        if (!await context.OfficeLocations.AnyAsync(o => o.CompanyId == acme.Id && o.Name == "Remote"))
        {
            context.OfficeLocations.Add(new OfficeLocation
            {
                Id = Guid.NewGuid(), Name = "Remote", Address = "N/A", City = "Remote", Country = "USA", CompanyId = acme.Id
            });
        }
        await context.SaveChangesAsync();

        // --- Second company: a healthcare agency client, to demonstrate multi-tenant isolation ---
        var meridian = await context.Companies.FirstOrDefaultAsync(c => c.Name == "Meridian Health Group");
        if (meridian is null)
        {
            meridian = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Meridian Health Group",
                Industry = "Healthcare",
                Description = "Demo second company — used to verify tenant data isolation (each company only sees its own jobs/reports)."
            };
            context.Companies.Add(meridian);
            await context.SaveChangesAsync();
        }

        var meridianClinical = await context.Departments.FirstOrDefaultAsync(d => d.CompanyId == meridian.Id && d.Name == "Clinical Operations");
        if (meridianClinical is null)
        {
            meridianClinical = new Department { Id = Guid.NewGuid(), Name = "Clinical Operations", CompanyId = meridian.Id };
            context.Departments.Add(meridianClinical);
            await context.SaveChangesAsync();
        }


        if (!await context.Designations.AnyAsync(d => d.DepartmentId == meridianClinical.Id))
        {
            context.Designations.Add(new Designation { Id = Guid.NewGuid(), Title = "Registered Nurse", DepartmentId = meridianClinical.Id });
        }
        if (!await context.OfficeLocations.AnyAsync(o => o.CompanyId == meridian.Id && o.Name == "Main Campus"))
        {
            context.OfficeLocations.Add(new OfficeLocation
            {
                Id = Guid.NewGuid(), Name = "Main Campus", Address = "500 Health Way", City = "Austin", Country = "USA", CompanyId = meridian.Id
            });
        }
        await context.SaveChangesAsync();



        // --- Staff users ---
        User MakeUser(string first, string last, string email, UserRoleType role, Guid? companyId) => new()
        {
            FirstName = first,
            LastName = last,
            Email = email,
            PasswordHash = passwordHasher.Hash("Demo@12345"),
            RoleId = roles[role].Id,
            CompanyId = companyId,
            IsEmailVerified = true,
            IsActive = true
        };

        var acmeHrManager = MakeUser("Priya", "Sharma", "hr@acme-demo.local", UserRoleType.HRManager, acme.Id);
        var acmeRecruiter = MakeUser("Daniel", "Cho", "recruiter@acme-demo.local", UserRoleType.Recruiter, acme.Id);
        var acmeInterviewer = MakeUser("Sofia", "Martinez", "interviewer@acme-demo.local", UserRoleType.Interviewer, acme.Id);
        var meridianRecruiter = MakeUser("James", "O'Brien", "recruiter@meridian-demo.local", UserRoleType.Recruiter, meridian.Id);
        context.Users.AddRange(acmeHrManager, acmeRecruiter, acmeInterviewer, meridianRecruiter);
        await context.SaveChangesAsync();

        // --- Jobs (published — visible on the job board immediately) ---
        Job MakeJob(string title, string description, Department dept, Company company, User hiringManager, User recruiter,
            string experience, decimal salaryMin, decimal salaryMax, string location, EmploymentType type, params string[] skills) => new()
        {
            Title = title,
            Description = description,
            Responsibilities = "Collaborate with cross-functional teams; own features end-to-end; participate in code/design review.",
            Benefits = "Health/dental/vision insurance, 401(k) match, flexible PTO, remote-friendly.",
            DepartmentId = dept.Id,
            CompanyId = company.Id,
            HiringManagerId = hiringManager.Id,
            CreatedByRecruiterId = recruiter.Id,
            ExperienceRequired = experience,
            SalaryMin = salaryMin,
            SalaryMax = salaryMax,
            Location = location,
            EmploymentType = type,
            Status = JobStatus.Published,
            JobSkills = skills.Select(s => new JobSkill { SkillName = s }).ToList()
        };

        var backendJob = MakeJob(
            "Senior Backend Engineer",
            "We're looking for a Senior Backend Engineer to design and build scalable APIs powering our core platform. You'll work closely with product and frontend teams to ship features that matter.",
            acmeEngineering, acme, acmeHrManager, acmeRecruiter, "5+ years", 130000, 170000, "Remote", EmploymentType.FullTime,
            "C#", "ASP.NET Core", "SQL Server", "Azure", "REST APIs");

        var frontendJob = MakeJob(
            "Frontend Developer",
            "Join our frontend team building fast, accessible React interfaces used by thousands of recruiters and candidates every day.",
            acmeEngineering, acme, acmeHrManager, acmeRecruiter, "2-4 years", 90000, 120000, "San Francisco, CA", EmploymentType.FullTime,
            "React", "TypeScript", "Tailwind CSS", "Redux");

        var pmJob = MakeJob(
            "Product Manager",
            "Own the roadmap for our AI-powered candidate matching features, from discovery through launch.",
            acmeProduct, acme, acmeHrManager, acmeRecruiter, "3-6 years", 110000, 150000, "Remote", EmploymentType.FullTime,
            "Product Strategy", "SQL", "A/B Testing", "Roadmapping");

        var nurseJob = MakeJob(
            "Registered Nurse — ICU",
            "Meridian Health Group is hiring an experienced ICU nurse for our main campus. Night and day shifts available.",
            meridianClinical, meridian, meridianRecruiter, meridianRecruiter, "3+ years", 75000, 95000, "Austin, TX", EmploymentType.FullTime,
            "ICU", "Critical Care", "BLS Certified", "ACLS Certified");

        context.Jobs.AddRange(backendJob, frontendJob, pmJob, nurseJob);
        await context.SaveChangesAsync();

        // --- Candidates ---
        (User user, Candidate candidate) MakeCandidate(
            string first, string last, string email, string headline, string summary, string employer,
            decimal expectedSalary, string linkedIn, string[] skills, (string company, string title, int startYearsAgo, int? endYearsAgo)[] experience,
            (string institution, string degree, string field, int startYear, int endYear)[] education)
        {
            var user = MakeUser(first, last, email, UserRoleType.Candidate, null);
            context.Users.Add(user);

            var candidate = new Candidate
            {
                UserId = user.Id,
                User = user,
                Headline = headline,
                Summary = summary,
                CurrentEmployer = employer,
                ExpectedSalary = expectedSalary,
                LinkedInUrl = linkedIn,
                Skills = skills.Select(s => new CandidateSkill { SkillName = s, ExtractedByAi = true, YearsOfExperience = Random.Shared.Next(1, 8) }).ToList(),
                Experiences = experience.Select(e => new Experience
                {
                    CompanyName = e.company,
                    Title = e.title,
                    StartDate = DateTime.UtcNow.AddYears(-e.startYearsAgo),
                    EndDate = e.endYearsAgo.HasValue ? DateTime.UtcNow.AddYears(-e.endYearsAgo.Value) : null,
                    Description = $"Worked as {e.title} at {e.company}."
                }).ToList(),
                Educations = education.Select(ed => new Education
                {
                    Institution = ed.institution, Degree = ed.degree, FieldOfStudy = ed.field, StartYear = ed.startYear, EndYear = ed.endYear
                }).ToList()
            };
            context.Candidates.Add(candidate);
            return (user, candidate);
        }

        var (janeUser, janeCandidate) = MakeCandidate(
            "Jane", "Doe", "jane.doe@example-demo.local", "Senior Backend Engineer | C# & Cloud",
            "Backend engineer with 6 years building scalable APIs and distributed systems on Azure.",
            "PrevTech Inc", 155000, "https://linkedin.com/in/janedoe-demo",
            new[] { "C#", "ASP.NET Core", "SQL Server", "Azure", "Docker", "REST APIs" },
            new[] { ("PrevTech Inc", "Backend Engineer", 4, (int?)null), ("StartupCo", "Junior Developer", 6, 4) },
            new[] { ("State University", "B.S.", "Computer Science", 2016, 2020) });

        var (markUser, markCandidate) = MakeCandidate(
            "Mark", "Chen", "mark.chen@example-demo.local", "Frontend Developer | React Specialist",
            "Frontend developer passionate about accessible, performant UI. 3 years of production React experience.",
            "WebCraft Studio", 105000, "https://linkedin.com/in/markchen-demo",
            new[] { "React", "TypeScript", "Tailwind CSS", "Redux", "HTML", "CSS" },
            new[] { ("WebCraft Studio", "Frontend Developer", 3, (int?)null) },
            new[] { ("Tech Community College", "A.S.", "Web Development", 2019, 2021) });

        var (priyaCandUser, priyaCandCandidate) = MakeCandidate(
            "Priya", "Nair", "priya.nair@example-demo.local", "Product Manager | B2B SaaS",
            "PM with 5 years shipping B2B SaaS features, strong in data-driven roadmapping and cross-functional leadership.",
            "SaaSCo", 135000, "https://linkedin.com/in/priyanair-demo",
            new[] { "Product Strategy", "SQL", "A/B Testing", "Roadmapping", "Jira" },
            new[] { ("SaaSCo", "Product Manager", 5, (int?)null) },
            new[] { ("Business School", "MBA", "Business Administration", 2017, 2019) });

        var (alexUser, alexCandidate) = MakeCandidate(
            "Alex", "Kim", "alex.kim@example-demo.local", "Full-Stack Developer",
            "Full-stack developer comfortable across the whole stack — React on the frontend, .NET on the backend.",
            "Freelance", 100000, "https://linkedin.com/in/alexkim-demo",
            new[] { "React", "C#", "ASP.NET Core", "TypeScript", "SQL Server" },
            new[] { ("Freelance", "Full-Stack Developer", 2, (int?)null) },
            new[] { ("Online University", "B.S.", "Software Engineering", 2018, 2022) });

        var (nurseUser, nurseCandidate) = MakeCandidate(
            "Rachel", "Adams", "rachel.adams@example-demo.local", "ICU Registered Nurse",
            "Experienced ICU nurse with 4 years of critical care experience across two Level I trauma centers.",
            "City General Hospital", 82000, "https://linkedin.com/in/racheladams-demo",
            new[] { "ICU", "Critical Care", "BLS Certified", "ACLS Certified" },
            new[] { ("City General Hospital", "ICU Nurse", 4, (int?)null) },
            new[] { ("Nursing College", "B.S.N.", "Nursing", 2018, 2022) });

        await context.SaveChangesAsync();

        // --- Applications with AI match scores ---
        Domain.Entities.Application MakeApplication(Job job, Candidate candidate, int matchScore, ApplicationStatus status,
            string[] missingSkills, string[] recommendedSkills, string recommendation) => new()
        {
            JobId = job.Id,
            CandidateId = candidate.Id,
            Status = status,
            MatchScore = matchScore,
            MissingSkillsJson = System.Text.Json.JsonSerializer.Serialize(missingSkills),
            RecommendedSkillsJson = System.Text.Json.JsonSerializer.Serialize(recommendedSkills),
            AiRecommendation = recommendation
        };

        var janeApplication = MakeApplication(backendJob, janeCandidate, 92, ApplicationStatus.TechnicalInterview,
            Array.Empty<string>(), new[] { "Kubernetes" }, "Excellent match — strong C#/Azure background directly aligned with role requirements.");

        var alexApplicationBackend = MakeApplication(backendJob, alexCandidate, 68, ApplicationStatus.Applied,
            new[] { "Azure", "Docker" }, new[] { "Azure", "Docker" }, "Reasonable full-stack background but lighter on cloud/infra experience than ideal.");

        var markApplication = MakeApplication(frontendJob, markCandidate, 88, ApplicationStatus.Shortlisted,
            Array.Empty<string>(), new[] { "Next.js" }, "Strong React fundamentals, solid fit for the role.");

        var alexApplicationFrontend = MakeApplication(frontendJob, alexCandidate, 75, ApplicationStatus.Applied,
            new[] { "Tailwind CSS" }, new[] { "Tailwind CSS" }, "Good general frontend skills, less specialized than other candidates.");

        var priyaApplication = MakeApplication(pmJob, priyaCandCandidate, 95, ApplicationStatus.Offer,
            Array.Empty<string>(), Array.Empty<string>(), "Outstanding match — directly relevant B2B SaaS PM experience.");

        var nurseApplication = MakeApplication(nurseJob, nurseCandidate, 90, ApplicationStatus.HRInterview,
            Array.Empty<string>(), Array.Empty<string>(), "Strong clinical background, meets all required certifications.");

        context.Applications.AddRange(janeApplication, alexApplicationBackend, markApplication, alexApplicationFrontend, priyaApplication, nurseApplication);
        await context.SaveChangesAsync();

        // --- Interview + feedback for Jane (Technical round) ---
        var janeRound = new InterviewRound { ApplicationId = janeApplication.Id, RoundName = "Technical", SequenceOrder = 1 };
        var markRound = new InterviewRound { ApplicationId = markApplication.Id, RoundName = "Screening", SequenceOrder = 1 };
        var priyaRound = new InterviewRound { ApplicationId = priyaApplication.Id, RoundName = "Executive", SequenceOrder = 2 };

        context.InterviewRounds.AddRange(janeRound, markRound, priyaRound);
        await context.SaveChangesAsync();

        var janeInterview = new Interview
        {
            InterviewRoundId = janeRound.Id,
            InterviewerId = acmeInterviewer.Id,
            ScheduledAtUtc = DateTime.UtcNow.AddDays(2),
            DurationMinutes = 60,
            MeetingLink = "https://meet.example-demo.local/jane-doe-technical",
            Result = InterviewResultStatus.Pending
        };

        var markInterview = new Interview
        {
            InterviewRoundId = markRound.Id,
            InterviewerId = acmeHrManager.Id,
            ScheduledAtUtc = DateTime.UtcNow.AddDays(1).AddHours(3),
            DurationMinutes = 45,
            MeetingLink = "https://meet.example-demo.local/mark-chen-screening",
            Result = InterviewResultStatus.Pending
        };

        var priyaInterview = new Interview
        {
            InterviewRoundId = priyaRound.Id,
            InterviewerId = acmeHrManager.Id,
            ScheduledAtUtc = DateTime.UtcNow.AddDays(-3),
            DurationMinutes = 60,
            MeetingLink = "https://meet.example-demo.local/priya-nair-exec",
            Result = InterviewResultStatus.Passed
        };

        context.Interviews.AddRange(janeInterview, markInterview, priyaInterview);
        await context.SaveChangesAsync();

        // Add feedback for Priya's passed interview
        context.Feedbacks.Add(new Feedback
        {
            InterviewId = priyaInterview.Id,
            Rating = 5,
            Strengths = "Exceptional strategic vision and deep understanding of SaaS metrics.",
            Weaknesses = "None observed.",
            Comments = "Strong recommendation to extend formal offer.",
            Recommend = true
        });
        await context.SaveChangesAsync();



        // --- Offer for Priya (PM role) ---
        context.Offers.Add(new Offer
        {
            ApplicationId = priyaApplication.Id,
            OfferedSalary = 140000,
            JoiningDate = DateTime.UtcNow.AddDays(21),
            Notes = "Includes signing bonus and standard equity grant per level.",
            IsAccepted = false
        });
        await context.SaveChangesAsync();

        logger.LogInformation("Demo data seeded: 2 companies, 4 jobs (published), 5 candidates, 6 applications, 1 interview, 1 offer.");
        logger.LogInformation("Demo logins (password: Demo@12345): hr@acme-demo.local, recruiter@acme-demo.local, " +
            "interviewer@acme-demo.local, recruiter@meridian-demo.local, jane.doe@example-demo.local, mark.chen@example-demo.local, " +
            "priya.nair@example-demo.local, alex.kim@example-demo.local, rachel.adams@example-demo.local");
    }
}
