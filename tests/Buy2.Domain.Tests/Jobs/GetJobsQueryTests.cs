using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.GetJobs;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class GetJobsQueryTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetJobsQuery_FiltersBySearchTerm_TitleAndDepartment()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var deptEng = new Department { Name = "Engineering" };
        var deptSales = new Department { Name = "Sales" };
        context.Departments.AddRange(deptEng, deptSales);
        await context.SaveChangesAsync();

        var job1 = new JobRole { Title = "Backend Developer", DepartmentId = deptEng.Id, SeniorityLevel = "Senior", AttendanceType = "Hybrid", IsActive = true };
        var job2 = new JobRole { Title = "Sales Representative", DepartmentId = deptSales.Id, SeniorityLevel = "Mid", AttendanceType = "OnSite", IsActive = true };
        context.JobRoles.AddRange(job1, job2);
        await context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(jobRepo);

        // Act - Search matching title
        var resultTitle = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto(SearchTerm: "backend")), CancellationToken.None);
        // Act - Search matching department
        var resultDept = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto(SearchTerm: "sales")), CancellationToken.None);

        // Assert
        Assert.Single(resultTitle.Items);
        Assert.Equal("Backend Developer", resultTitle.Items[0].Title);

        Assert.Single(resultDept.Items);
        Assert.Equal("Sales Representative", resultDept.Items[0].Title);
    }

    [Fact]
    public async Task GetJobsQuery_FiltersByDepartment_Seniority_WorkModel_And_IsActive()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var deptEng = new Department { Name = "Engineering" };
        var deptHR = new Department { Name = "Human Resources" };
        context.Departments.AddRange(deptEng, deptHR);
        await context.SaveChangesAsync();

        var job1 = new JobRole { Title = "Senior Lead", DepartmentId = deptEng.Id, SeniorityLevel = "Senior", AttendanceType = "Remote", IsActive = true };
        var job2 = new JobRole { Title = "HR Specialist", DepartmentId = deptHR.Id, SeniorityLevel = "Junior", AttendanceType = "OnSite", IsActive = false };
        context.JobRoles.AddRange(job1, job2);
        await context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(jobRepo);

        // Act & Assert DepartmentId
        var deptResult = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto(DepartmentId: deptEng.Id)), CancellationToken.None);
        Assert.Single(deptResult.Items);
        Assert.Equal("Senior Lead", deptResult.Items[0].Title);

        // Act & Assert SeniorityLevel
        var seniorityResult = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto(SeniorityLevel: "Junior")), CancellationToken.None);
        Assert.Single(seniorityResult.Items);
        Assert.Equal("HR Specialist", seniorityResult.Items[0].Title);

        // Act & Assert WorkModel
        var workModelResult = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto(WorkModel: "Remote")), CancellationToken.None);
        Assert.Single(workModelResult.Items);
        Assert.Equal("Senior Lead", workModelResult.Items[0].Title);

        // Act & Assert IsActive
        var activeResult = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto(IsActive: false)), CancellationToken.None);
        Assert.Single(activeResult.Items);
        Assert.Equal("HR Specialist", activeResult.Items[0].Title);
    }

    [Fact]
    public async Task GetJobsQuery_CalculatesAssignedEmployeeCount_AndRequiredQualificationsCount()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var job = new JobRole
        {
            Title = "Fullstack Engineer",
            SeniorityLevel = "Mid",
            AttendanceType = "Hybrid",
            IsActive = true,
            RequiredQualificationsJson = "[\"C#\", \".NET\", \"React\"]"
        };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var emp1 = new Employee { FirstName = "Alice", LastName = "W", JobRoleId = job.Id, IsDeleted = false };
        var emp2 = new Employee { FirstName = "Bob", LastName = "M", JobRoleId = job.Id, IsDeleted = false };
        var empDeleted = new Employee { FirstName = "Charlie", LastName = "D", JobRoleId = job.Id, IsDeleted = true };
        context.Employees.AddRange(emp1, emp2, empDeleted);
        await context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(jobRepo);

        // Act
        var result = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto()), CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].AssignedEmployeesCount);
        Assert.Equal(3, result.Items[0].RequiredQualificationsCount);
    }

    [Fact]
    public async Task GetJobsQuery_PaginationAndSorting_CalculatesCorrectPages()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        for (int i = 1; i <= 15; i++)
        {
            context.JobRoles.Add(new JobRole
            {
                Title = $"Job {i:D2}",
                SeniorityLevel = "Mid",
                AttendanceType = "Remote",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(jobRepo);

        // Act
        var page1 = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto(PageNumber: 1, PageSize: 10, SortBy: "title", SortDir: "asc")), CancellationToken.None);
        var page2 = await handler.Handle(new GetJobsQuery(new JobFilterQueryDto(PageNumber: 2, PageSize: 10, SortBy: "title", SortDir: "asc")), CancellationToken.None);

        // Assert
        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
        Assert.Equal("Job 01", page1.Items[0].Title);
        Assert.Equal("Job 11", page2.Items[0].Title);
    }
}
