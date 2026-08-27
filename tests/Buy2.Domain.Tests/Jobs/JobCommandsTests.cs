using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Features.Jobs.DTOs;
using Buy2.Application.Features.Jobs;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Jobs;

public class JobCommandsTests
{
    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task CreateJobCommandHandler_DuplicateTitleInDepartment_ShouldThrowException()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        var dept = new Department { Name = "IT", IsActive = true };
        context.Departments.Add(dept);
        
        var job = new JobRole { Title = "Software Engineer", DepartmentId = 1, IsActive = true };
        context.JobRoles.Add(job);
        
        await context.SaveChangesAsync();

        var command = new CreateJobCommand(new CreateJobDto(
            Title: "Software Engineer",
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: "Senior",
            Description: "Desc",
            RequiredQualifications: new List<string> { "C#" },
            ExperienceYearsMin: 5,
            WorkModel: "OnSite",
            OnlineWorkdays: new List<string>(),
            OfflineWorkdays: new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }
        ));

        var handler = new CreateJobCommandHandler(jobRepo, deptRepo, uow);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateJobCommandHandler_InlineDepartmentResolution_ShouldCreateNewDepartment()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        var command = new CreateJobCommand(new CreateJobDto(
            Title: "HR Manager",
            DepartmentId: null,
            NewDepartmentName: "Human Resources",
            SeniorityLevel: "Senior",
            Description: "HR Desc",
            RequiredQualifications: new List<string> { "HR" },
            ExperienceYearsMin: 5,
            WorkModel: "OnSite",
            OnlineWorkdays: new List<string>(),
            OfflineWorkdays: new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }
        ));

        var handler = new CreateJobCommandHandler(jobRepo, deptRepo, uow);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var newDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Human Resources");
        Assert.NotNull(newDept);
        Assert.Equal(newDept.Id, result.DepartmentId);
    }

    [Fact]
    public async Task CreateJobCommandHandler_OnSiteWithOnlineDays_ShouldThrowException()
    {
        // Arrange
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        var command = new CreateJobCommand(new CreateJobDto(
            Title: "Software Engineer",
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: "Senior",
            Description: "Desc",
            RequiredQualifications: new List<string>(),
            ExperienceYearsMin: 5,
            WorkModel: "OnSite",
            OnlineWorkdays: new List<string> { "Monday" },
            OfflineWorkdays: new List<string> { "Tuesday" }
        ));

        var handler = new CreateJobCommandHandler(jobRepo, deptRepo, uow);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateJobCommandHandler_RemoteWithOfflineDays_ShouldThrowException()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        var command = new CreateJobCommand(new CreateJobDto(
            Title: "Software Engineer",
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: "Senior",
            Description: "Desc",
            RequiredQualifications: new List<string>(),
            ExperienceYearsMin: 5,
            WorkModel: "Remote",
            OnlineWorkdays: new List<string> { "Monday" },
            OfflineWorkdays: new List<string> { "Tuesday" }
        ));

        var handler = new CreateJobCommandHandler(jobRepo, deptRepo, uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateJobCommandHandler_HybridDaysNotMatchingActiveWorkWeek_ShouldThrowException()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        var command = new CreateJobCommand(new CreateJobDto(
            Title: "Software Engineer",
            DepartmentId: 1,
            NewDepartmentName: null,
            SeniorityLevel: "Senior",
            Description: "Desc",
            RequiredQualifications: new List<string>(),
            ExperienceYearsMin: 5,
            WorkModel: "Hybrid",
            OnlineWorkdays: new List<string> { "Monday" },
            OfflineWorkdays: new List<string> { "Tuesday" }
        ));

        var handler = new CreateJobCommandHandler(jobRepo, deptRepo, uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }
    [Fact]
    public async Task UpdateJobCommandHandler_NonExistentJobId_ShouldThrowException()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        var dto = new UpdateJobDto(
            Title: "Test",
            DepartmentId: 1,
            SeniorityLevel: "Junior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 1,
            WorkModel: "Remote",
            OnlineWorkdays: null,
            OfflineWorkdays: null,
            IsActive: true);

        var command = new UpdateJobCommand(999, dto);
        var handler = new UpdateJobCommandHandler(jobRepo, deptRepo, uow);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateJobCommandHandler_DuplicateTitleInDepartment_ShouldThrowException()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        context.Departments.Add(new Department { Name = "IT", IsActive = true });
        context.JobRoles.Add(new JobRole { Title = "Software Engineer", DepartmentId = 1, IsActive = true });
        context.JobRoles.Add(new JobRole { Title = "QA Engineer", DepartmentId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var dto = new UpdateJobDto(
            Title: "Software Engineer", // conflicting with existing
            DepartmentId: 1,
            SeniorityLevel: "Junior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 1,
            WorkModel: "Remote",
            OnlineWorkdays: null,
            OfflineWorkdays: null,
            IsActive: true);

        var command = new UpdateJobCommand(2, dto);
        var handler = new UpdateJobCommandHandler(jobRepo, deptRepo, uow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateJobCommandHandler_OnSiteWithOnlineDays_ShouldThrowException()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        context.JobRoles.Add(new JobRole { Title = "Software Engineer", DepartmentId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var dto = new UpdateJobDto(
            Title: "Software Engineer",
            DepartmentId: 1,
            SeniorityLevel: "Junior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 1,
            WorkModel: "OnSite",
            OnlineWorkdays: new List<string> { "Monday" },
            OfflineWorkdays: null,
            IsActive: true);

        var command = new UpdateJobCommand(1, dto);
        var handler = new UpdateJobCommandHandler(jobRepo, deptRepo, uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateJobCommandHandler_RemoteWithOfflineDays_ShouldThrowException()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        context.JobRoles.Add(new JobRole { Title = "Software Engineer", DepartmentId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var dto = new UpdateJobDto(
            Title: "Software Engineer",
            DepartmentId: 1,
            SeniorityLevel: "Junior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 1,
            WorkModel: "Remote",
            OnlineWorkdays: null,
            OfflineWorkdays: new List<string> { "Monday" },
            IsActive: true);

        var command = new UpdateJobCommand(1, dto);
        var handler = new UpdateJobCommandHandler(jobRepo, deptRepo, uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateJobCommandHandler_HybridDaysNotMatchingActiveWorkWeek_ShouldThrowException()
    {
        using var context = CreateDbContext();
        var jobRepo = new GenericRepository<JobRole>(context);
        var deptRepo = new GenericRepository<Department>(context);
        var uow = new UnitOfWork(context);

        context.JobRoles.Add(new JobRole { Title = "Software Engineer", DepartmentId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var dto = new UpdateJobDto(
            Title: "Software Engineer",
            DepartmentId: 1,
            SeniorityLevel: "Junior",
            Description: null,
            RequiredQualifications: null,
            ExperienceYearsMin: 1,
            WorkModel: "Hybrid",
            OnlineWorkdays: new List<string> { "Monday", "Tuesday" },
            OfflineWorkdays: new List<string> { "Wednesday", "Thursday" }, // Total 4 days
            IsActive: true);

        var command = new UpdateJobCommand(1, dto);
        var handler = new UpdateJobCommandHandler(jobRepo, deptRepo, uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }
}
