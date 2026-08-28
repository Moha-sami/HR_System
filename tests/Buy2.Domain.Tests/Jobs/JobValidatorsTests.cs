using System.Collections.Generic;
using Buy2.Application.Features.Departments.DTOs;
using Buy2.Application.Features.Departments.Validators;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.Validators;
using Buy2.Application.Features.Qualifications.DTOs;
using Buy2.Application.Features.Qualifications.Validators;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class JobValidatorsTests
{
    private readonly CreateJobDtoValidator _createJobValidator = new();
    private readonly UpdateJobDtoValidator _updateJobValidator = new();
    private readonly ReassignEmployeesAndDeleteJobDtoValidator _reassignAndDeleteValidator = new();
    private readonly CreateDepartmentDtoValidator _createDepartmentValidator = new();
    private readonly CreateQualificationDtoValidator _createQualificationValidator = new();

    #region CreateJobDtoValidator Tests

    [Fact]
    public void CreateJobDtoValidator_ValidPayload_WithDepartmentId_ShouldPass()
    {
        var dto = new CreateJobDto(
            Title: "Software Engineer",
            DepartmentId: 10,
            NewDepartmentName: null,
            SeniorityLevel: "Senior",
            Description: "Backend engineer role",
            RequiredQualifications: new List<string> { "C#", ".NET" },
            ExperienceYearsMin: 3,
            WorkModel: "Hybrid",
            OnlineWorkdays: new List<string> { "Monday" },
            OfflineWorkdays: new List<string> { "Tuesday" }
        );

        var result = _createJobValidator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void CreateJobDtoValidator_ValidPayload_WithNewDepartmentName_ShouldPass()
    {
        var dto = new CreateJobDto(
            Title: "DevOps Engineer",
            DepartmentId: null,
            NewDepartmentName: "Cloud Infrastructure",
            SeniorityLevel: "Lead",
            Description: "Cloud operations",
            RequiredQualifications: null,
            ExperienceYearsMin: 5,
            WorkModel: "Remote",
            OnlineWorkdays: null,
            OfflineWorkdays: null
        );

        var result = _createJobValidator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")] // Length 1 (< 2)
    public void CreateJobDtoValidator_InvalidTitle_ShouldFail(string? title)
    {
        var dto = new CreateJobDto(
            Title: title!,
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: "Junior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 0,
            WorkModel: "OnSite",
            OnlineWorkdays: null,
            OfflineWorkdays: null
        );

        var result = _createJobValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateJobDto.Title));
    }

    [Fact]
    public void CreateJobDtoValidator_TitleExceeds100Chars_ShouldFail()
    {
        var longTitle = new string('T', 101);
        var dto = new CreateJobDto(
            Title: longTitle,
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: "Junior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 0,
            WorkModel: "OnSite",
            OnlineWorkdays: null,
            OfflineWorkdays: null
        );

        var result = _createJobValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateJobDto.Title));
    }

    [Fact]
    public void CreateJobDtoValidator_MissingDepartmentSelection_ShouldFail()
    {
        var dto = new CreateJobDto(
            Title: "QA Engineer",
            DepartmentId: null,
            NewDepartmentName: "   ",
            SeniorityLevel: "MidLevel",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 2,
            WorkModel: "OnSite",
            OnlineWorkdays: null,
            OfflineWorkdays: null
        );

        var result = _createJobValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Either DepartmentId or NewDepartmentName must be specified"));
    }

    [Theory]
    [InlineData("InvalidLevel")]
    [InlineData("")]
    [InlineData(null)]
    public void CreateJobDtoValidator_InvalidSeniorityLevel_ShouldFail(string? seniorityLevel)
    {
        var dto = new CreateJobDto(
            Title: "Project Manager",
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: seniorityLevel!,
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 3,
            WorkModel: "OnSite",
            OnlineWorkdays: null,
            OfflineWorkdays: null
        );

        var result = _createJobValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateJobDto.SeniorityLevel));
    }

    [Theory]
    [InlineData("Virtual")]
    [InlineData("Flexible")]
    [InlineData(null)]
    public void CreateJobDtoValidator_InvalidWorkModel_ShouldFail(string? workModel)
    {
        var dto = new CreateJobDto(
            Title: "Product Manager",
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: "Senior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 3,
            WorkModel: workModel!,
            OnlineWorkdays: null,
            OfflineWorkdays: null
        );

        var result = _createJobValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateJobDto.WorkModel));
    }

    [Fact]
    public void CreateJobDtoValidator_NegativeExperienceYearsMin_ShouldFail()
    {
        var dto = new CreateJobDto(
            Title: "Data Analyst",
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: "Junior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: -1,
            WorkModel: "OnSite",
            OnlineWorkdays: null,
            OfflineWorkdays: null
        );

        var result = _createJobValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateJobDto.ExperienceYearsMin));
    }

    #endregion

    #region UpdateJobDtoValidator Tests

    [Fact]
    public void UpdateJobDtoValidator_ValidPayload_ShouldPass()
    {
        var dto = new UpdateJobDto(
            Title: "Senior Software Engineer",
            DepartmentId: 5,
            SeniorityLevel: "Senior",
            Description: "Updated description",
            RequiredQualifications: new List<string> { "C#" },
            ExperienceYearsMin: 5,
            WorkModel: "Hybrid",
            OnlineWorkdays: null,
            OfflineWorkdays: null,
            IsActive: true
        );

        var result = _updateJobValidator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-2)]
    public void UpdateJobDtoValidator_InvalidDepartmentId_ShouldFail(int? deptId)
    {
        var dto = new UpdateJobDto(
            Title: "Software Engineer",
            DepartmentId: deptId,
            SeniorityLevel: "MidLevel",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 2,
            WorkModel: "OnSite",
            OnlineWorkdays: null,
            OfflineWorkdays: null,
            IsActive: true
        );

        var result = _updateJobValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateJobDto.DepartmentId));
    }

    [Fact]
    public void UpdateJobDtoValidator_InvalidTitle_ShouldFail()
    {
        var dto = new UpdateJobDto(
            Title: "A",
            DepartmentId: 1,
            SeniorityLevel: "MidLevel",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 2,
            WorkModel: "OnSite",
            OnlineWorkdays: null,
            OfflineWorkdays: null,
            IsActive: true
        );

        var result = _updateJobValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateJobDto.Title));
    }

    [Theory]
    [InlineData("Ab")] // Length 2 (Lower bound)
    public void UpdateJobDtoValidator_TitleLengthLowerBound_ShouldPass(string title)
    {
        var dto = new UpdateJobDto(title, 1, "MidLevel", null, null, 2, "OnSite", null, null, true);
        Assert.True(_updateJobValidator.Validate(dto).IsValid);
    }

    [Fact]
    public void UpdateJobDtoValidator_TitleLength100_ShouldPass_And101_ShouldFail()
    {
        var title100 = new string('A', 100);
        var dto100 = new UpdateJobDto(title100, 1, "MidLevel", null, null, 2, "OnSite", null, null, true);
        Assert.True(_updateJobValidator.Validate(dto100).IsValid);

        var title101 = new string('A', 101);
        var dto101 = new UpdateJobDto(title101, 1, "MidLevel", null, null, 2, "OnSite", null, null, true);
        Assert.False(_updateJobValidator.Validate(dto101).IsValid);
    }
    
    [Theory]
    [InlineData("junior")] // Case insensitivity
    [InlineData("JUNIOR")]
    [InlineData("Invalid")]
    [InlineData("   ")] // Whitespace
    [InlineData(null)]
    public void UpdateJobDtoValidator_SeniorityLevelValidation(string? level)
    {
        var dto = new UpdateJobDto("Developer", 1, level ?? "", null, null, 2, "OnSite", null, null, true);
        var result = _updateJobValidator.Validate(dto);
        if (string.IsNullOrWhiteSpace(level) || level == "Invalid") Assert.False(result.IsValid);
        else Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("onsite")] // Case insensitivity
    [InlineData("ONSITE")]
    [InlineData("Invalid")]
    public void UpdateJobDtoValidator_WorkModelValidation(string model)
    {
        var dto = new UpdateJobDto("Developer", 1, "Junior", null, null, 2, model, null, null, true);
        var result = _updateJobValidator.Validate(dto);
        if (model == "Invalid") Assert.False(result.IsValid);
        else Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)] // Boundary
    [InlineData(-1)]
    public void UpdateJobDtoValidator_ExperienceYearsMinValidation(int exp)
    {
        var dto = new UpdateJobDto("Developer", 1, "Junior", null, null, exp, "OnSite", null, null, true);
        var result = _updateJobValidator.Validate(dto);
        if (exp >= 0) Assert.True(result.IsValid);
        else Assert.False(result.IsValid);
    }

    #endregion

    #region ReassignEmployeesAndDeleteJobDtoValidator Tests

    [Fact]
    public void ReassignEmployeesAndDeleteJobDtoValidator_ValidReplacementJobId_ShouldPass()
    {
        var dto = new ReassignEmployeesAndDeleteJobDto(ReplacementJobId: 42);

        var result = _reassignAndDeleteValidator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ReassignEmployeesAndDeleteJobDtoValidator_InvalidReplacementJobId_ShouldFail(int replacementJobId)
    {
        var dto = new ReassignEmployeesAndDeleteJobDto(ReplacementJobId: replacementJobId);

        var result = _reassignAndDeleteValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReassignEmployeesAndDeleteJobDto.ReplacementJobId));
    }

    #endregion

    #region CreateDepartmentDtoValidator Tests

    [Fact]
    public void CreateDepartmentDtoValidator_ValidPayload_ShouldPass()
    {
        var dto = new CreateDepartmentDto(
            Name: "Human Resources",
            Code: "HR-01",
            Description: "People operations department",
            HeadEmployeeId: 12
        );

        var result = _createDepartmentValidator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("X")] // Length 1
    public void CreateDepartmentDtoValidator_InvalidName_ShouldFail(string? name)
    {
        var dto = new CreateDepartmentDto(
            Name: name!,
            Code: "HR",
            Description: null,
            HeadEmployeeId: null
        );

        var result = _createDepartmentValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateDepartmentDto.Name));
    }

    [Fact]
    public void CreateDepartmentDtoValidator_CodeExceeds20Chars_ShouldFail()
    {
        var longCode = new string('C', 21);
        var dto = new CreateDepartmentDto(
            Name: "Engineering",
            Code: longCode,
            Description: null,
            HeadEmployeeId: null
        );

        var result = _createDepartmentValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateDepartmentDto.Code));
    }

    #endregion

    #region CreateQualificationDtoValidator Tests

    [Fact]
    public void CreateQualificationDtoValidator_ValidPayload_ShouldPass()
    {
        var dto = new CreateQualificationDto(
            Name: "AWS Certified Solutions Architect",
            Type: "Cloud & DevOps",
            Description: "Professional cloud certification"
        );

        var result = _createQualificationValidator.Validate(dto);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Q")] // Length 1 (< 2)
    public void CreateQualificationDtoValidator_InvalidName_ShouldFail(string? name)
    {
        var dto = new CreateQualificationDto(
            Name: name!,
            Type: "Certifications",
            Description: null
        );

        var result = _createQualificationValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateQualificationDto.Name));
    }

    [Fact]
    public void CreateQualificationDtoValidator_NameExceeds150Chars_ShouldFail()
    {
        var longName = new string('Q', 151);
        var dto = new CreateQualificationDto(
            Name: longName,
            Type: "Certifications",
            Description: null
        );

        var result = _createQualificationValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateQualificationDto.Name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateQualificationDtoValidator_InvalidCategory_ShouldFail(string? Type)
    {
        var dto = new CreateQualificationDto(
            Name: "Scrum Master",
            Type: Type!,
            Description: null
        );

        var result = _createQualificationValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateQualificationDto.Type));
    }

    [Fact]
    public void CreateQualificationDtoValidator_CategoryExceeds50Chars_ShouldFail()
    {
        var longCategory = new string('C', 51);
        var dto = new CreateQualificationDto(
            Name: "Scrum Master",
            Type: longCategory,
            Description: null
        );

        var result = _createQualificationValidator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateQualificationDto.Type));
    }

    #endregion
}
