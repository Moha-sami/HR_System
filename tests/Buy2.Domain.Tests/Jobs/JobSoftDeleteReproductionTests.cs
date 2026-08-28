using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs.GetJobById;
using Buy2.Application.Features.Jobs.GetJobs;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class JobSoftDeleteReproductionTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task JobRole_DbContextAndQueries_ShouldFilterOutSoftDeletedJobRolesByDefault()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);

        var dept = new Department { Name = "Engineering", IsActive = true };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        var activeJob = new JobRole
        {
            Title = "Senior Backend Engineer",
            DepartmentId = dept.Id,
            SeniorityLevel = "Senior",
            AttendanceType = "Hybrid",
            IsActive = true,
            IsDeleted = false
        };

        var softDeletedJob = new JobRole
        {
            Title = "Former QA Engineer",
            DepartmentId = dept.Id,
            SeniorityLevel = "Mid",
            AttendanceType = "OnSite",
            IsActive = true,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow
        };

        context.JobRoles.AddRange(activeJob, softDeletedJob);
        await context.SaveChangesAsync();

        // 1. Direct DbContext Query assertion - JobRole entity should have a global query filter (!j.IsDeleted)
        // just like Employee entity has modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted)
        var jobsFromDb = await context.JobRoles.ToListAsync();
        Assert.DoesNotContain(jobsFromDb, j => j.Id == softDeletedJob.Id);

        // 2. GetJobsQuery assertion - GetJobs endpoint should not return soft-deleted jobs
        var getJobsHandler = new GetJobsQueryHandler(jobRepo);
        var jobsListResult = await getJobsHandler.Handle(new GetJobsQuery(new JobFilterQueryDto()), CancellationToken.None);
        Assert.DoesNotContain(jobsListResult.Items, j => j.Id == softDeletedJob.Id);

        // 3. GetJobByIdQuery assertion - GetJobById endpoint should return null (404) for soft-deleted jobs
        var getJobByIdHandler = new GetJobByIdQueryHandler(jobRepo);
        var jobByIdResult = await getJobByIdHandler.Handle(new GetJobByIdQuery(softDeletedJob.Id), CancellationToken.None);
        Assert.Null(jobByIdResult);
    }
}
