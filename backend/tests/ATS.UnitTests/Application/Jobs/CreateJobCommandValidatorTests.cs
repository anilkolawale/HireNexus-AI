using ATS.Application.Features.Jobs.Commands;
using ATS.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace ATS.UnitTests.Application.Jobs;

public class CreateJobCommandValidatorTests
{
    private readonly CreateJobCommandValidator _validator = new();

    private static CreateJobCommand ValidCommand() => new(
        Title: "Senior .NET Developer",
        Description: "Build great software.",
        Responsibilities: null,
        Benefits: null,
        DepartmentId: Guid.NewGuid(),
        CompanyId: Guid.NewGuid(),
        HiringManagerId: Guid.NewGuid(),
        CreatedByRecruiterId: Guid.NewGuid(),
        ExperienceRequired: "3-5 years",
        SalaryMin: 80000,
        SalaryMax: 120000,
        Location: "Remote",
        EmploymentType: EmploymentType.FullTime,
        Skills: new List<string> { "C#", "ASP.NET Core" });

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Title_FailsValidation()
    {
        var command = ValidCommand() with { Title = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void SalaryMax_LessThan_SalaryMin_FailsValidation()
    {
        var command = ValidCommand() with { SalaryMin = 100000, SalaryMax = 50000 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SalaryMax);
    }

    [Fact]
    public void No_Skills_FailsValidation()
    {
        var command = ValidCommand() with { Skills = new List<string>() };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Skills);
    }
}
