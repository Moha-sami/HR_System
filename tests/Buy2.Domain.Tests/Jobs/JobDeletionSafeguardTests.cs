using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.Jobs;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Buy2.Application.Common.Interfaces;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class JobDeletionSafeguardTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task GetJobDeletionImpactQuery_ZeroEmployees_ReturnsCanDeleteDirectlyTrue()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);

        var job = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var handler = new GetJobDeletionImpactQueryHandler(jobRepo, empRepo);
        var result = await handler.Handle(new GetJobDeletionImpactQuery(job.Id), CancellationToken.None);

        Assert.True(result.CanDeleteDirectly);
        Assert.Empty(result.AffectedEmployees);
    }


    [Fact]
    public async Task GetJobDeletionImpactQuery_WithEmployees_ReturnsCanDeleteDirectlyFalse()
    {
        using var context = CreateDbContext();
        var job = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();

        var employee = new Employee { FirstName = "John", LastName = "Doe", Email = "test@example.com", IsActive = true, JobRoleId = job.Id, JobRole = job };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        
        context.ChangeTracker.Clear();

        var sanityCheck = await context.Employees.ToListAsync();
        Assert.Single(sanityCheck);
        var sanityJobCheck = await context.JobRoles.Include(j => j.Employees).ToListAsync();
        Assert.Single(sanityJobCheck.First().Employees);

        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new GetJobDeletionImpactQueryHandler(jobRepo, empRepo);
        var result = await handler.Handle(new GetJobDeletionImpactQuery(job.Id), CancellationToken.None);

        Assert.False(result.CanDeleteDirectly);
        Assert.Single(result.AffectedEmployees);
        Assert.Equal(employee.Id, result.AffectedEmployees.First().Id);
    }

    [Fact]
    public async Task GetJobDeletionImpactQuery_JobNotFound_ThrowsKeyNotFoundException()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new GetJobDeletionImpactQueryHandler(jobRepo, empRepo);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new GetJobDeletionImpactQuery(99), CancellationToken.None));
    }

    [Fact]
    public async Task ReassignAndDeleteJobCommand_Valid_ReassignsAndSoftDeletes()
    {
        using var context = CreateDbContext();
        var replacementJob = new JobRole { Title = "Job 2", DepartmentId = 1, IsActive = true };
        var jobToDelete = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
        context.JobRoles.AddRange(jobToDelete, replacementJob);
        await context.SaveChangesAsync();

        var employee = new Employee { FirstName = "John", LastName = "Doe", Email = "test@example.com", IsActive = true, JobRoleId = jobToDelete.Id, JobRole = jobToDelete };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);

        var handler = new ReassignAndDeleteJobCommandHandler(jobRepo, empRepo, uow);
        await handler.Handle(new ReassignAndDeleteJobCommand(jobToDelete.Id, replacementJob.Id), CancellationToken.None);

        var deletedJob = await context.JobRoles.IgnoreQueryFilters().FirstOrDefaultAsync(j => j.Id == jobToDelete.Id);
        Assert.NotNull(deletedJob);
        Assert.True(deletedJob.IsDeleted);
        Assert.NotNull(deletedJob.DeletedAt);

        var updatedEmployee = await context.Employees.FindAsync(employee.Id);
        Assert.NotNull(updatedEmployee);
        Assert.Equal(replacementJob.Id, updatedEmployee.JobRoleId);
    }

    [Fact]
    public async Task ReassignAndDeleteJobCommand_NoReplacementProvided_ThrowsInvalidOperationException()
    {
        using var context = CreateDbContext();
        var jobToDelete = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
        context.JobRoles.Add(jobToDelete);
        await context.SaveChangesAsync();
        
        var employee = new Employee { FirstName = "John", LastName = "Doe", Email = "test@example.com", IsActive = true, JobRoleId = jobToDelete.Id, JobRole = jobToDelete };
        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);

        var handler = new ReassignAndDeleteJobCommandHandler(jobRepo, empRepo, uow);
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new ReassignAndDeleteJobCommand(jobToDelete.Id, null), CancellationToken.None));
    }

    [Fact]
    public async Task ReassignAndDeleteJobCommand_SameJobId_ThrowsArgumentException()
    {
        using var context = CreateDbContext();
        var jobToDelete = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
        context.JobRoles.Add(jobToDelete);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var uow = new UnitOfWork(context);

        var handler = new ReassignAndDeleteJobCommandHandler(jobRepo, empRepo, uow);
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new ReassignAndDeleteJobCommand(jobToDelete.Id, jobToDelete.Id), CancellationToken.None));
    }

    [Fact]
    public async Task GetJobDeletionImpactQuery_WithInactiveOrDeletedEmployees_IgnoresThem()
    {
        using var context = CreateDbContext();
        var job = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();
        
        var empInactive = new Employee { FirstName = "A", LastName = "B", Email = "a@a.com", IsActive = false, JobRoleId = job.Id, JobRole = job };
        var empActive = new Employee { FirstName = "E", LastName = "F", Email = "c@c.com", IsActive = true, JobRoleId = job.Id, JobRole = job };
        
        context.Employees.Add(empInactive);
        context.Employees.Add(empActive);
        await context.SaveChangesAsync();
        
        context.ChangeTracker.Clear();

        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new GetJobDeletionImpactQueryHandler(jobRepo, empRepo);
        var result = await handler.Handle(new GetJobDeletionImpactQuery(job.Id), CancellationToken.None);

        Assert.False(result.CanDeleteDirectly);
        Assert.Single(result.AffectedEmployees);
        Assert.Equal(empActive.Id, result.AffectedEmployees.First().Id);
    }

    [Fact]
    public async Task GetJobDeletionImpactQuery_EmployeeWithNullValues_HandlesCoalescingAndTrimming()
    {
        using var context = CreateDbContext();
        var job = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
        context.JobRoles.Add(job);
        await context.SaveChangesAsync();
        
        var emp = new Employee { FirstName = " E ", LastName = " F ", Email = "e@f.com", IsActive = true, JobRoleId = job.Id, JobRole = job };
        context.Employees.Add(emp);
        await context.SaveChangesAsync();
        
        context.ChangeTracker.Clear();

        var jobRepo = new GenericRepository<JobRole>(context);
        var empRepo = new GenericRepository<Employee>(context);
        var handler = new GetJobDeletionImpactQueryHandler(jobRepo, empRepo);
        var result = await handler.Handle(new GetJobDeletionImpactQuery(job.Id), CancellationToken.None);

        var affected = result.AffectedEmployees.First();
        Assert.Equal("E F", affected.FullName);
        Assert.Equal(string.Empty, affected.SiteName);
    }

        [Fact]
        public async Task ReassignAndDeleteJobCommand_JobNotFound_ThrowsKeyNotFoundException()
        {
            using var context = CreateDbContext();
            var jobRepo = new GenericRepository<JobRole>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var uow = new UnitOfWork(context);
            var handler = new ReassignAndDeleteJobCommandHandler(jobRepo, empRepo, uow);
            
            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new ReassignAndDeleteJobCommand(99, null), CancellationToken.None));
        }

        [Fact]
        public async Task ReassignAndDeleteJobCommand_ReplacementJobNotFound_ThrowsKeyNotFoundException()
        {
            using var context = CreateDbContext();
            var job = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
            context.JobRoles.Add(job);
            
            var emp = new Employee { FirstName = "A", LastName = "B", IsActive = true, IsDeleted = false, JobRole = job };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var jobRepo = new GenericRepository<JobRole>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var uow = new UnitOfWork(context);
            var handler = new ReassignAndDeleteJobCommandHandler(jobRepo, empRepo, uow);
            
            await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(new ReassignAndDeleteJobCommand(job.Id, 99), CancellationToken.None));
        }

        [Fact]
        public async Task ReassignAndDeleteJobCommand_NoActiveEmployees_DeletesDirectlyWithoutReassigning()
        {
            using var context = CreateDbContext();
            var job = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
            context.JobRoles.Add(job);
            
            var empInactive = new Employee { FirstName = "A", LastName = "B", IsActive = false, IsDeleted = false, JobRole = job };
            context.Employees.Add(empInactive);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var jobRepo = new GenericRepository<JobRole>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var uow = new UnitOfWork(context);
            var handler = new ReassignAndDeleteJobCommandHandler(jobRepo, empRepo, uow);
            
            // Should succeed without replacement job ID
            await handler.Handle(new ReassignAndDeleteJobCommand(job.Id, null), CancellationToken.None);

            var deletedJob = await context.JobRoles.IgnoreQueryFilters().FirstOrDefaultAsync(j => j.Id == job.Id);
            Assert.NotNull(deletedJob);
            Assert.True(deletedJob.IsDeleted);
        }

        [Fact]
        public async Task GetJobDeletionImpactQuery_MultipleJobs_OnlyReturnsImpactForRequestedJob()
        {
            using var context = CreateDbContext();
            var job1 = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
            var job2 = new JobRole { Title = "Job 2", DepartmentId = 1, IsActive = true };
            context.JobRoles.AddRange(job1, job2);
            await context.SaveChangesAsync();
            
            var emp1 = new Employee { FirstName = "A", LastName = "B", Email = "a@b.com", IsActive = true, JobRoleId = job1.Id, JobRole = job1 };
            var emp2 = new Employee { FirstName = "C", LastName = "D", Email = "c@d.com", IsActive = true, JobRoleId = job2.Id, JobRole = job2 };
            context.Employees.AddRange(emp1, emp2);
            await context.SaveChangesAsync();
            
            context.ChangeTracker.Clear();

            var jobRepo = new GenericRepository<JobRole>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var handler = new GetJobDeletionImpactQueryHandler(jobRepo, empRepo);
            var result = await handler.Handle(new GetJobDeletionImpactQuery(job1.Id), CancellationToken.None);

            Assert.False(result.CanDeleteDirectly);
            Assert.Single(result.AffectedEmployees);
            Assert.Equal(emp1.Id, result.AffectedEmployees.First().Id);
        }

        [Fact]
        public async Task GetJobDeletionImpactQuery_EmployeeWithNullNames_HandlesGracefully()
        {
            using var context = CreateDbContext();
            var job = new JobRole { Title = "Job 1", DepartmentId = 1, IsActive = true };
            context.JobRoles.Add(job);
            await context.SaveChangesAsync();
            
            var emp = new Employee { FirstName = null, LastName = null, Email = "test@example.com", IsActive = true, JobRoleId = job.Id, JobRole = job };
            context.Employees.Add(emp);
            await context.SaveChangesAsync();
            
            context.ChangeTracker.Clear();

            var jobRepo = new GenericRepository<JobRole>(context);
            var empRepo = new GenericRepository<Employee>(context);
            var handler = new GetJobDeletionImpactQueryHandler(jobRepo, empRepo);
            var result = await handler.Handle(new GetJobDeletionImpactQuery(job.Id), CancellationToken.None);

            var affected = result.AffectedEmployees.First();
            Assert.Equal(string.Empty, affected.FullName);
        }
}
