using System.Text.Json;
using Buy2.Application.DTOs.Employees;
using Buy2.Domain.Enums;
using Xunit;

namespace Buy2.Domain.Tests.Employees;

public class EmployeeProfileTests
{
    [Fact]
    public void EmployeePersonalInfoDto_SupportsEnrichedFields()
    {
        var birthdate = new DateTime(1995, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var dto = new EmployeePersonalInfoDto(
            Name: "Jane Doe",
            Birthdate: birthdate,
            Email: "jane.doe@buy2.com",
            PhoneNumber: "+1-555-0123",
            Gender: Gender.Female
        );

        Assert.Equal("Jane Doe", dto.Name);
        Assert.Equal(birthdate, dto.Birthdate);
        Assert.Equal("jane.doe@buy2.com", dto.Email);
        Assert.Equal("+1-555-0123", dto.PhoneNumber);
        Assert.Equal(Gender.Female, dto.Gender);
    }

    [Fact]
    public void EmployeePersonalInfoDto_NullableBirthdateSupported()
    {
        var dto = new EmployeePersonalInfoDto(
            Name: "John Smith",
            Birthdate: null,
            Email: "john.smith@buy2.com",
            PhoneNumber: "+1-555-0199",
            Gender: Gender.Male
        );

        Assert.Equal("John Smith", dto.Name);
        Assert.Null(dto.Birthdate);
        Assert.Equal("john.smith@buy2.com", dto.Email);
        Assert.Equal("+1-555-0199", dto.PhoneNumber);
        Assert.Equal(Gender.Male, dto.Gender);
    }

    [Fact]
    public void EmployeeJobDetailsDto_SupportsAttendanceAndWorkdays()
    {
        var qualifications = new List<string> { "C#", "SQL", "EF Core" };
        var onlineDays = new List<string> { "Monday", "Wednesday" };
        var offlineDays = new List<string> { "Tuesday", "Thursday", "Friday" };

        var dto = new EmployeeJobDetailsDto(
            Title: "Senior Software Engineer",
            Department: "Department #1",
            SeniorityLevel: "Senior",
            ExperienceYears: 7,
            DirectManagerName: "Alice Smith",
            JobType: "FullTime",
            Qualifications: qualifications,
            AttendanceType: "Hybrid",
            OnlineWorkdays: onlineDays,
            OfflineWorkdays: offlineDays,
            JobRoleId: 10,
            DirectManagerId: 5
        );

        Assert.Equal("Senior Software Engineer", dto.Title);
        Assert.Equal("Department #1", dto.Department);
        Assert.Equal("Senior", dto.SeniorityLevel);
        Assert.Equal(7, dto.ExperienceYears);
        Assert.Equal("Alice Smith", dto.DirectManagerName);
        Assert.Equal("FullTime", dto.JobType);
        Assert.Equal(qualifications, dto.Qualifications);
        Assert.Equal("Hybrid", dto.AttendanceType);
        Assert.Equal(onlineDays, dto.OnlineWorkdays);
        Assert.Equal(offlineDays, dto.OfflineWorkdays);
        Assert.Equal(10, dto.JobRoleId);
        Assert.Equal(5, dto.DirectManagerId);
    }

    [Fact]
    public void EmployeePayrollSummaryDto_InitializesCorrectly()
    {
        var assignedSiteIds = new List<int> { 1, 2, 3 };

        var dto = new EmployeePayrollSummaryDto(
            SalaryType: "Fixed",
            PaymentAmount: 5000.00m,
            PayoutPeriod: "Monthly",
            PayoutDay: 25,
            WorkWeekStartDay: "Sunday",
            WorkWeekEndDay: "Thursday",
            OvertimeEnabled: true,
            OvertimeThresholdHours: 40.0m,
            OvertimeRateMultiplier: 1.5m,
            AssignedWorkSiteIds: assignedSiteIds
        );

        Assert.Equal("Fixed", dto.SalaryType);
        Assert.Equal(5000.00m, dto.PaymentAmount);
        Assert.Equal("Monthly", dto.PayoutPeriod);
        Assert.Equal(25, dto.PayoutDay);
        Assert.Equal("Sunday", dto.WorkWeekStartDay);
        Assert.Equal("Thursday", dto.WorkWeekEndDay);
        Assert.True(dto.OvertimeEnabled);
        Assert.Equal(40.0m, dto.OvertimeThresholdHours);
        Assert.Equal(1.5m, dto.OvertimeRateMultiplier);
        Assert.Equal(assignedSiteIds, dto.AssignedWorkSiteIds);
    }

    [Fact]
    public void EmployeeProfileDto_FullEnrichedStructure_SerializesToJsonProperly()
    {
        var stats = new EmployeeStatsDto(TotalPoints: 100, TotalTasks: 5, TotalGifts: 2);
        var personalInfo = new EmployeePersonalInfoDto(
            Name: "John Connor",
            Birthdate: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Email: "john.connor@buy2.com",
            PhoneNumber: "+1-555-0042",
            Gender: Gender.Male
        );
        var jobDetails = new EmployeeJobDetailsDto(
            Title: "Tech Lead",
            Department: "Department #3",
            SeniorityLevel: "Lead",
            ExperienceYears: 10,
            DirectManagerName: "Sarah Connor",
            JobType: "FullTime",
            Qualifications: new List<string> { "Leadership", "Architecture" },
            AttendanceType: "OnSite",
            OnlineWorkdays: new List<string>(),
            OfflineWorkdays: new List<string> { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday" }
        );
        var payroll = new EmployeePayrollSummaryDto(
            SalaryType: "Fixed",
            PaymentAmount: 8000.00m,
            PayoutPeriod: "Monthly",
            PayoutDay: 28,
            WorkWeekStartDay: "Sunday",
            WorkWeekEndDay: "Thursday",
            OvertimeEnabled: false,
            OvertimeThresholdHours: null,
            OvertimeRateMultiplier: null,
            AssignedWorkSiteIds: new List<int> { 1 }
        );

        var profile = new EmployeeProfileDto(
            Id: 42,
            EmployeeCode: "EMP-0042",
            FullName: "John Connor",
            Phone: "+1-555-0042",
            Email: "john.connor@buy2.com",
            Location: "HQ Campus",
            ProfilePhotoUrl: "https://storage.buy2.com/photos/emp-0042.jpg",
            Stats: stats,
            PersonalInfo: personalInfo,
            JobDetails: jobDetails,
            Payroll: payroll
        );

        var json = JsonSerializer.Serialize(profile);
        Assert.NotNull(json);
        Assert.Contains("EMP-0042", json);
        Assert.Contains("John Connor", json);
        Assert.Contains("john.connor@buy2.com", json);
        Assert.Contains("Tech Lead", json);
        Assert.Contains("8000", json);
    }
}
