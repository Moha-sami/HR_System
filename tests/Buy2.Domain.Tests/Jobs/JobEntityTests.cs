using System;
using Buy2.Domain.Entities;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class JobEntityTests
{
    [Fact]
    public void JobRole_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Act
        var jobRole = new JobRole();

        // Assert
        Assert.Equal(string.Empty, jobRole.Title);
        Assert.Null(jobRole.DepartmentId);
        Assert.Null(jobRole.Department);
        Assert.Null(jobRole.Description);
        Assert.Equal("Junior", jobRole.SeniorityLevel);
        Assert.Equal("[]", jobRole.RequiredQualificationsJson);
        Assert.Equal(0, jobRole.ExperienceYears);
        Assert.Equal("OnSite", jobRole.AttendanceType);
        Assert.Equal("[]", jobRole.OnlineWorkdaysJson);
        Assert.Equal("[]", jobRole.OfflineWorkdaysJson);
        Assert.True(jobRole.IsActive);
        Assert.True(jobRole.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(jobRole.UpdatedAt);
        Assert.NotNull(jobRole.Employees);
        Assert.Empty(jobRole.Employees);
    }

    [Fact]
    public void JobRole_PropertyAssignments_ShouldWorkAsExpected()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var dept = new Department { Name = "Engineering" };
        var emp = new Employee { FirstName = "John", LastName = "Doe" };

        // Act
        var jobRole = new JobRole
        {
            Title = "Senior Developer",
            DepartmentId = 5,
            Department = dept,
            Description = "Software developer role",
            SeniorityLevel = "Senior",
            RequiredQualificationsJson = "[\"C#\", \"EF Core\"]",
            ExperienceYears = 5,
            AttendanceType = "Hybrid",
            OnlineWorkdaysJson = "[\"Mon\", \"Tue\"]",
            OfflineWorkdaysJson = "[\"Wed\", \"Thu\", \"Fri\"]",
            IsActive = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        jobRole.Employees.Add(emp);

        // Assert
        Assert.Equal("Senior Developer", jobRole.Title);
        Assert.Equal(5, jobRole.DepartmentId);
        Assert.Equal(dept, jobRole.Department);
        Assert.Equal("Software developer role", jobRole.Description);
        Assert.Equal("Senior", jobRole.SeniorityLevel);
        Assert.Equal("[\"C#\", \"EF Core\"]", jobRole.RequiredQualificationsJson);
        Assert.Equal(5, jobRole.ExperienceYears);
        Assert.Equal("Hybrid", jobRole.AttendanceType);
        Assert.Equal("[\"Mon\", \"Tue\"]", jobRole.OnlineWorkdaysJson);
        Assert.Equal("[\"Wed\", \"Thu\", \"Fri\"]", jobRole.OfflineWorkdaysJson);
        Assert.False(jobRole.IsActive);
        Assert.Equal(now, jobRole.CreatedAt);
        Assert.Equal(now, jobRole.UpdatedAt);
        Assert.Single(jobRole.Employees);
        Assert.Contains(emp, jobRole.Employees);
    }

    [Fact]
    public void Department_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Act
        var dept = new Department();

        // Assert
        Assert.Equal(1, dept.OrganizationId);
        Assert.Equal(string.Empty, dept.Name);
        Assert.Null(dept.Code);
        Assert.Null(dept.Description);
        Assert.Null(dept.HeadEmployeeId);
        Assert.Null(dept.HeadEmployee);
        Assert.True(dept.IsActive);
        Assert.True(dept.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(dept.UpdatedAt);
        Assert.NotNull(dept.JobRoles);
        Assert.Empty(dept.JobRoles);
        Assert.NotNull(dept.Employees);
        Assert.Empty(dept.Employees);
    }

    [Fact]
    public void Department_PropertyAssignments_ShouldWorkAsExpected()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var headEmp = new Employee { FirstName = "Manager", LastName = "One" };
        var jobRole = new JobRole { Title = "Dev" };
        var emp = new Employee { FirstName = "Dev", LastName = "One" };

        // Act
        var dept = new Department
        {
            OrganizationId = 2,
            Name = "Human Resources",
            Code = "HR",
            Description = "HR Dept",
            HeadEmployeeId = 10,
            HeadEmployee = headEmp,
            IsActive = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        dept.JobRoles.Add(jobRole);
        dept.Employees.Add(emp);

        // Assert
        Assert.Equal(2, dept.OrganizationId);
        Assert.Equal("Human Resources", dept.Name);
        Assert.Equal("HR", dept.Code);
        Assert.Equal("HR Dept", dept.Description);
        Assert.Equal(10, dept.HeadEmployeeId);
        Assert.Equal(headEmp, dept.HeadEmployee);
        Assert.False(dept.IsActive);
        Assert.Equal(now, dept.CreatedAt);
        Assert.Equal(now, dept.UpdatedAt);
        Assert.Single(dept.JobRoles);
        Assert.Contains(jobRole, dept.JobRoles);
        Assert.Single(dept.Employees);
        Assert.Contains(emp, dept.Employees);
    }

    [Fact]
    public void Qualification_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Act
        var qual = new Qualification();

        // Assert
        Assert.Equal(string.Empty, qual.Name);
        Assert.Equal(string.Empty, qual.Category);
        Assert.Null(qual.Description);
        Assert.True(qual.IsActive);
        Assert.True(qual.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Qualification_PropertyAssignments_ShouldWorkAsExpected()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var qual = new Qualification
        {
            Name = "AWS Certified Developer",
            Category = "Certification",
            Description = "Cloud dev certification",
            IsActive = false,
            CreatedAt = now
        };

        // Assert
        Assert.Equal("AWS Certified Developer", qual.Name);
        Assert.Equal("Certification", qual.Category);
        Assert.Equal("Cloud dev certification", qual.Description);
        Assert.False(qual.IsActive);
        Assert.Equal(now, qual.CreatedAt);
    }
}
