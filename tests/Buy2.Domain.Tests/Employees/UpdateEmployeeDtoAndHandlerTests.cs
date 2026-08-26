using Buy2.Application.Features.Employees.UpdateJobDetails;
using Buy2.Application.Features.Employees.UpdateJobDetails.Validators;
using Buy2.Application.Features.Employees.UpdatePayrollProfile;
using Buy2.Application.Features.Employees.UpdatePersonalInfo;
using Buy2.Application.Features.Employees.UpdatePersonalInfo.Validators;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Xunit;

namespace Buy2.Domain.Tests.Employees;

public class UpdateEmployeeDtoAndHandlerTests
{
    [Fact]
    public void UpdateEmployeePersonalInfoDto_InitializesCorrectly()
    {
        var birthdate = new DateTimeOffset(1990, 5, 20, 0, 0, 0, TimeSpan.Zero);
        var dto = new UpdateEmployeePersonalInfoDto(
            FirstName: "John",
            LastName: "Doe",
            PhoneNumber: "+1234567890",
            Birthdate: birthdate,
            Gender: Gender.Male,
            Email: "john.doe@example.com"
        );

        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("+1234567890", dto.PhoneNumber);
        Assert.Equal(birthdate, dto.Birthdate);
        Assert.Equal(Gender.Male, dto.Gender);
        Assert.Equal("john.doe@example.com", dto.Email);
    }

    [Fact]
    public void UpdateEmployeePersonalInfoDtoValidator_ValidPayload_Passes()
    {
        var validator = new UpdateEmployeePersonalInfoDtoValidator();
        var dto = new UpdateEmployeePersonalInfoDto(
            FirstName: "Jane",
            LastName: "Smith",
            PhoneNumber: "1234567",
            Birthdate: DateTimeOffset.UtcNow.AddYears(-25),
            Gender: Gender.Female,
            Email: "jane.smith@example.com"
        );

        var result = validator.Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateEmployeePersonalInfoDtoValidator_InvalidEmailAndFutureBirthdate_Fails()
    {
        var validator = new UpdateEmployeePersonalInfoDtoValidator();
        var dto = new UpdateEmployeePersonalInfoDto(
            Birthdate: DateTimeOffset.UtcNow.AddDays(10),
            Email: "not-an-email"
        );

        var result = validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateEmployeePersonalInfoDto.Email));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateEmployeePersonalInfoDto.Birthdate));
    }

    [Fact]
    public void UpdateJobDetailsDto_InitializesCorrectly()
    {
        var onlineDays = new List<string> { "Monday", "Wednesday" };
        var offlineDays = new List<string> { "Tuesday", "Thursday" };
        var quals = new List<string> { "C#", ".NET" };

        var dto = new UpdateJobDetailsDto(
            JobRoleId: 1,
            DirectManagerId: 2,
            SeniorityLevel: "Senior",
            ExperienceYears: 5,
            JobType: "FullTime",
            AttendanceType: "Hybrid",
            OnlineWorkdays: onlineDays,
            OfflineWorkdays: offlineDays,
            Qualifications: quals
        );

        Assert.Equal(1, dto.JobRoleId);
        Assert.Equal(2, dto.DirectManagerId);
        Assert.Equal("Senior", dto.SeniorityLevel);
        Assert.Equal(5, dto.ExperienceYears);
        Assert.Equal("FullTime", dto.JobType);
        Assert.Equal("Hybrid", dto.AttendanceType);
        Assert.Equal(onlineDays, dto.OnlineWorkdays);
        Assert.Equal(offlineDays, dto.OfflineWorkdays);
        Assert.Equal(quals, dto.Qualifications);
    }

    [Fact]
    public void UpdateJobDetailsDtoValidator_ValidPayload_Passes()
    {
        var validator = new UpdateJobDetailsDtoValidator();
        var dto = new UpdateJobDetailsDto(
            JobRoleId: 5,
            DirectManagerId: 10,
            ExperienceYears: 3
        );

        var result = validator.Validate(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateJobDetailsDtoValidator_NegativeValues_Fails()
    {
        var validator = new UpdateJobDetailsDtoValidator();
        var dto = new UpdateJobDetailsDto(
            JobRoleId: -1,
            DirectManagerId: 0,
            ExperienceYears: -5
        );

        var result = validator.Validate(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateJobDetailsDto.JobRoleId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateJobDetailsDto.DirectManagerId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateJobDetailsDto.ExperienceYears));
    }

    [Fact]
    public void UpdatePayrollProfileDto_StandardizedFieldNames_InitializesCorrectly()
    {
        var assignedSites = new List<int> { 1, 2 };
        var onlineDays = new List<string> { "Monday" };

        var dto = new UpdatePayrollProfileDto(
            SalaryType: SalaryType.Fixed,
            PayoutPeriod: "Monthly",
            PayoutDay: 25,
            WorkWeekStartDay: DayOfWeek.Sunday,
            WorkWeekEndDay: DayOfWeek.Thursday,
            PaymentAmount: 6000.00m,
            OvertimeThresholdHours: 40m,
            OvertimeRateMultiplier: 1.5m,
            AttendanceType: "OnSite",
            AssignedWorkSiteIds: assignedSites,
            OnlineWorkdays: onlineDays,
            OfflineWorkdays: null
        );

        Assert.Equal(SalaryType.Fixed, dto.SalaryType);
        Assert.Equal("Monthly", dto.PayoutPeriod);
        Assert.Equal(25, dto.PayoutDay);
        Assert.Equal(DayOfWeek.Sunday, dto.WorkWeekStartDay);
        Assert.Equal(DayOfWeek.Thursday, dto.WorkWeekEndDay);
        Assert.Equal(6000.00m, dto.PaymentAmount);
        Assert.Equal(40m, dto.OvertimeThresholdHours);
        Assert.Equal(1.5m, dto.OvertimeRateMultiplier);
        Assert.Equal("OnSite", dto.AttendanceType);
        Assert.Equal(assignedSites, dto.AssignedWorkSiteIds);
        Assert.Equal(onlineDays, dto.OnlineWorkdays);
    }
}
