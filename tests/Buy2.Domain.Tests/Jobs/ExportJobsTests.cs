using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.Jobs;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.ExportJobs;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class ExportJobsAndReassignmentTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task ExportJobsQuery_ReturnsValidCsvWithBom()
    {
        using var context = CreateDbContext();
        var dept = new Department { Name = "Engineering", Code = "ENG" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var job1 = new JobRole
        {
            Title = "Backend Developer",
            DepartmentId = dept.Id,
            Department = dept,
            SeniorityLevel = "Senior",
            AttendanceType = "Hybrid",
            ExperienceYears = 5,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var job2 = new JobRole
        {
            Title = "Frontend Developer",
            DepartmentId = dept.Id,
            Department = dept,
            SeniorityLevel = "MidLevel",
            AttendanceType = "Remote",
            ExperienceYears = 3,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.JobRoles.AddRange(job1, job2);
        await context.SaveChangesAsync();

        var jobRepo = new GenericRepository<JobRole>(context);
        var handler = new ExportJobsQueryHandler(jobRepo);

        var result = await handler.Handle(new ExportJobsQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Length > 3);

        // Verify UTF-8 BOM
        var bom = Encoding.UTF8.GetPreamble();
        Assert.Equal(bom[0], result[0]);
        Assert.Equal(bom[1], result[1]);
        Assert.Equal(bom[2], result[2]);

        var csvText = Encoding.UTF8.GetString(result, bom.Length, result.Length - bom.Length);
        Assert.Contains("Job Title,Department,Seniority Level,Work Model,Experience (Years),Assigned Employees,Status,Created Date", csvText);
        Assert.Contains("Backend Developer", csvText);
        Assert.Contains("Frontend Developer", csvText);
        Assert.Contains("Engineering", csvText);
        Assert.Contains("Active", csvText);
        Assert.Contains("Inactive", csvText);
    }

    [Fact]
    public async Task ExportJobsQuery_WithSearchFilter_FiltersCorrectly()
    {
        using var context = CreateDbContext();
        var job1 = new JobRole { Title = "DevOps Engineer", SeniorityLevel = "Senior", AttendanceType = "OnSite", IsActive = true };
        var job2 = new JobRole { Title = "HR Specialist", SeniorityLevel = "Junior", AttendanceType = "Remote", IsActive = true };
        context.JobRoles.AddRange(job1, job2);
        await context.SaveChangesAsync();

        var jobRepo = new GenericRepository<JobRole>(context);
        var handler = new ExportJobsQueryHandler(jobRepo);

        var result = await handler.Handle(new ExportJobsQuery(SearchTerm: "DevOps"), CancellationToken.None);
        var csvText = Encoding.UTF8.GetString(result);

        Assert.Contains("DevOps Engineer", csvText);
        Assert.DoesNotContain("HR Specialist", csvText);
    }

    [Fact]
    public async Task ReassignAndDeleteJobCommand_WithGranularPerEmployeeReassignments_ReassignsAccurately()
    {
        using var context = CreateDbContext();
        var oldJob = new JobRole { Title = "Old Role", IsActive = true };
        var targetJobA = new JobRole { Title = "Role A", IsActive = true };
        var targetJobB = new JobRole { Title = "Role B", IsActive = true };
        context.JobRoles.AddRange(oldJob, targetJobA, targetJobB);
        await context.SaveChangesAsync();

        var emp1 = new Employee { FirstName = "Emp", LastName = "One", IsActive = true, JobRoleId = oldJob.Id, JobRole = oldJob };
        var emp2 = new Employee { FirstName = "Emp", LastName = "Two", IsActive = true, JobRoleId = oldJob.Id, JobRole = oldJob };
        context.Employees.AddRange(emp1, emp2);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);
        var handler = new ReassignAndDeleteJobCommandHandler(jobRepo, empRepo, uow);

        var reassignments = new List<EmployeeJobReassignmentDto>
        {
            new EmployeeJobReassignmentDto(emp1.Id, targetJobA.Id),
            new EmployeeJobReassignmentDto(emp2.Id, targetJobB.Id)
        };

        var response = await handler.Handle(new ReassignAndDeleteJobCommand(oldJob.Id, Reassignments: reassignments), CancellationToken.None);

        Assert.Equal(2, response.ReassignedCount);
        Assert.Equal(oldJob.Id, response.DeletedJobId);

        var updatedOldJob = await context.JobRoles.IgnoreQueryFilters().FirstAsync(j => j.Id == oldJob.Id);
        Assert.True(updatedOldJob.IsDeleted);

        var updatedEmp1 = await context.Employees.FirstAsync(e => e.Id == emp1.Id);
        var updatedEmp2 = await context.Employees.FirstAsync(e => e.Id == emp2.Id);
        Assert.Equal(targetJobA.Id, updatedEmp1.JobRoleId);
        Assert.Equal(targetJobB.Id, updatedEmp2.JobRoleId);
    }
}
