using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.GetJobById;
using Buy2.Application.Features.Jobs.GetJobEmployees;
using Buy2.Application.Features.Jobs.GetJobs;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class JobQueriesTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetJobsQuery_FiltersBySearchTerm_Department_Attendance_And_IsActive()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var deptEngineering = new Department { Name = "Engineering" };
        var deptSales = new Department { Name = "Sales" };
        context.Departments.AddRange(deptEngineering, deptSales);
        await context.SaveChangesAsync();

        var job1 = new JobRole
        {
            Title = "Senior Software Engineer",
            DepartmentId = deptEngineering.Id,
            SeniorityLevel = "Senior",
            ExperienceYears = 5,
            AttendanceType = "Hybrid",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var job2 = new JobRole
        {
            Title = "Sales Manager",
            DepartmentId = deptSales.Id,
            SeniorityLevel = "Manager",
            ExperienceYears = 7,
            AttendanceType = "OnSite",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var job3 = new JobRole
        {
            Title = "Junior Software Engineer",
            DepartmentId = deptEngineering.Id,
            SeniorityLevel = "Junior",
            ExperienceYears = 1,
            AttendanceType = "Remote",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        context.JobRoles.AddRange(job1, job2, job3);
        await context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(jobRepo);

        // Act & Assert 1: SearchTerm filter
        var searchQuery = new GetJobsQuery(new JobFilterQueryDto(SearchTerm: "Software"));
        var searchResult = await handler.Handle(searchQuery, CancellationToken.None);
        Assert.Equal(2, searchResult.TotalCount);
        Assert.Contains(searchResult.Items, j => j.Title == "Senior Software Engineer");
        Assert.Contains(searchResult.Items, j => j.Title == "Junior Software Engineer");

        // Act & Assert 2: DepartmentId filter
        var deptQuery = new GetJobsQuery(new JobFilterQueryDto(DepartmentId: deptSales.Id));
        var deptResult = await handler.Handle(deptQuery, CancellationToken.None);
        Assert.Single(deptResult.Items);
        Assert.Equal("Sales Manager", deptResult.Items[0].Title);

        // Act & Assert 3: AttendanceType filter
        var attendanceQuery = new GetJobsQuery(new JobFilterQueryDto(AttendanceType: "Hybrid"));
        var attendanceResult = await handler.Handle(attendanceQuery, CancellationToken.None);
        Assert.Single(attendanceResult.Items);
        Assert.Equal("Senior Software Engineer", attendanceResult.Items[0].Title);

        // Act & Assert 4: IsActive filter
        var activeQuery = new GetJobsQuery(new JobFilterQueryDto(IsActive: false));
        var activeResult = await handler.Handle(activeQuery, CancellationToken.None);
        Assert.Single(activeResult.Items);
        Assert.Equal("Junior Software Engineer", activeResult.Items[0].Title);
    }

    [Fact]
    public async Task GetJobsQuery_Pagination_And_AllocatedEmployeesCount()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var dept = new Department { Name = "Operations" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var job = new JobRole
        {
            Title = "Operations Lead",
            DepartmentId = dept.Id,
            SeniorityLevel = "Lead",
            ExperienceYears = 4,
            AttendanceType = "OnSite",
            IsActive = true
        };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var empActive1 = new Employee { FirstName = "John", LastName = "Doe", JobRoleId = job.Id, IsDeleted = false };
        var empActive2 = new Employee { FirstName = "Jane", LastName = "Smith", JobRoleId = job.Id, IsDeleted = false };
        var empDeleted = new Employee { FirstName = "Bob", LastName = "Brown", JobRoleId = job.Id, IsDeleted = true };
        context.Employees.AddRange(empActive1, empActive2, empDeleted);
        await context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(jobRepo);
        var query = new GetJobsQuery(new JobFilterQueryDto(PageNumber: 1, PageSize: 10));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].AllocatedEmployeesCount);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task GetJobByIdQuery_ReturnsJobDetails_WithParsedJson_WhenFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var dept = new Department { Name = "HR" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var job = new JobRole
        {
            Title = "HR Business Partner",
            Description = "Manages HR relations",
            DepartmentId = dept.Id,
            SeniorityLevel = "Senior",
            ExperienceYears = 6,
            AttendanceType = "Hybrid",
            OnlineWorkdaysJson = "[\"Monday\", \"Wednesday\"]",
            OfflineWorkdaysJson = "[\"Tuesday\", \"Thursday\", \"Friday\"]",
            RequiredQualificationsJson = "[\"Bachelor Degree\", \"SHRM-CP\"]",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var handler = new GetJobByIdQueryHandler(jobRepo);
        var query = new GetJobByIdQuery(job.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal("HR Business Partner", result.Title);
        Assert.Equal("Manages HR relations", result.Description);
        Assert.Equal("HR", result.DepartmentName);
        Assert.Equal(new List<string> { "Monday", "Wednesday" }, result.OnlineWorkdays);
        Assert.Equal(new List<string> { "Tuesday", "Thursday", "Friday" }, result.OfflineWorkdays);
        Assert.Equal(new List<string> { "Bachelor Degree", "SHRM-CP" }, result.RequiredQualifications);
    }

    [Fact]
    public async Task GetJobByIdQuery_ReturnsNull_WhenJobNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var handler = new GetJobByIdQueryHandler(jobRepo);

        // Act
        var result = await handler.Handle(new GetJobByIdQuery(999), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetJobEmployeesQuery_ReturnsNull_WhenJobNotFound()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);

        // Act
        var result = await handler.Handle(new GetJobEmployeesQuery(999), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetJobEmployeesQuery_ReturnsEmptyRoster_WhenNoEmployeesAssigned()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var job = new JobRole { Title = "Empty Role", IsActive = true };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);

        // Act
        var result = await handler.Handle(new GetJobEmployeesQuery(job.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetJobEmployeesQuery_ReturnsMappedRoster_WhenEmployeesExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var dept = new Department { Name = "IT" };
        var site = new Site { SiteName = "HQ Main Site" };
        context.Departments.Add(dept);
        context.Sites.Add(site);
        await context.SaveChangesAsync();

        var job = new JobRole { Title = "DevOps Engineer", DepartmentId = dept.Id, IsActive = true };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var emp = new Employee
        {
            FirstName = "Alice",
            LastName = "Smith",
            EmployeeCode = "EMP-0100",
            Email = "alice@example.com",
            PhoneNumber = "+123456789",
            JobRoleId = job.Id,
            SiteId = site.Id,
            JoinDate = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            IsActive = true,
            IsDeleted = false
        };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();

        var handler = new GetJobEmployeesQueryHandler(jobRepo, empRepo);

        // Act
        var result = await handler.Handle(new GetJobEmployeesQuery(job.Id), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);

        var item = result.Items[0];
        Assert.Equal(emp.Id, item.EmployeeId);
        Assert.Equal("EMP-0100", item.EmployeeCode);
        Assert.Equal("Alice Smith", item.FullName);
        Assert.Equal("alice@example.com", item.Email);
        Assert.Equal("+123456789", item.Phone);
        Assert.Equal("HQ Main Site", item.SiteName);
        Assert.Equal("IT", item.DepartmentName);
        Assert.Equal("Active", item.Status);
    }
}
