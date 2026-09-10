using Buy2.Application.DTOs.Schedules;
using Buy2.Application.Features.Schedules.ApplyTemplate;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Schedules;

public class ApplyTemplateTests
{
    private static readonly DateOnly TargetDate = new(2026, 9, 10); // Thursday

    private Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Buy2DbContext(options);
    }

    private static ApplyTemplateCommandHandler CreateHandler(Buy2DbContext context)
    {
        return new ApplyTemplateCommandHandler(
            new GenericRepository<Site>(context),
            new GenericRepository<ShiftTemplate>(context),
            new GenericRepository<ShiftEntity>(context),
            new GenericRepository<Employee>(context),
            new GenericRepository<JobRole>(context),
            new GenericRepository<EmployeeSite>(context),
            new GenericRepository<SiteOperationalHour>(context),
            new GenericRepository<Request>(context),
            new GenericRepository<AttendanceRecord>(context),
            new UnitOfWork(context));
    }

    private static Site SeedSite(Buy2DbContext context)
    {
        var site = new Site { Id = 3, SiteName = "Downtown Hub" };
        context.Sites.Add(site);
        context.JobRoles.Add(new JobRole { Id = 2, Title = "Cashier" });
        context.SaveChanges();
        return site;
    }

    private static Employee SeedEmployee(
        Buy2DbContext context,
        int id = 7,
        int jobRoleId = 2,
        decimal hourlyRate = 20m,
        string? onlineJson = null,
        string? offlineJson = null,
        decimal overtimeThreshold = 0m,
        decimal overtimeRate = 0m)
    {
        var employee = new Employee
        {
            Id = id,
            FirstName = "John",
            LastName = "Doe",
            JobRoleId = jobRoleId,
            IsActive = true,
            IsDeleted = false,
            OnlineWorkdaysJson = onlineJson,
            OfflineWorkdaysJson = offlineJson,
            PayrollProfile = new PayrollProfile
            {
                SalaryType = "Hourly",
                PaymentAmount = hourlyRate,
                OvertimeThresholdHours = overtimeThreshold,
                OvertimeHourlyRate = overtimeRate
            }
        };
        context.Employees.Add(employee);
        context.SaveChanges();
        return employee;
    }

    private static ShiftTemplate SeedTemplate(
        Buy2DbContext context,
        int id = 10,
        int? assignedUserId = 7,
        int jobRoleId = 2)
    {
        var template = new ShiftTemplate
        {
            Id = id,
            Name = "Morning Shift Template",
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            ShiftBlocks = new List<ShiftBlock>
            {
                new()
                {
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(13, 0, 0),
                    JobRoleId = jobRoleId,
                    EmployeeId = assignedUserId
                }
            }
        };
        context.ShiftTemplates.Add(template);
        context.SaveChanges();
        return template;
    }

    private static void Authorize(Buy2DbContext context, int employeeId, int siteId = 3)
    {
        context.EmployeeSites.Add(new EmployeeSite { EmployeeId = employeeId, SiteId = siteId });
        context.SaveChanges();
    }

    private static void SeedRequestType(Buy2DbContext context)
    {
        if (!context.RequestTypes.Any())
        {
            context.RequestTypes.Add(new RequestType { Id = 1, Name = "Annual Leave", Category = "Leave" });
            context.SaveChanges();
        }
    }

    [Fact]
    public async Task Handle_SiteNotFound_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(999, TargetDate, 10), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Handle_TemplateNotFound_ReturnsNotFound()
    {
        using var context = CreateDbContext();
        SeedSite(context);

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 999), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Handle_InvalidKeep_ReturnsValidationFailure()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedTemplate(context);

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10, "delete-everything"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationError);
    }

    [Fact]
    public async Task Handle_AuthorizedEmployee_KeepsAssignmentAndRecalculates()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var response = result.Value!;
        var block = Assert.Single(response.Blocks);
        Assert.Equal(7, block.EmployeeId);
        Assert.Equal("John Doe", block.EmployeeName);
        Assert.False(block.IsPublished);
        Assert.False(block.IsOpenRole);
        Assert.False(block.Stripped);
        Assert.Empty(response.Warnings);
        Assert.Equal(80m, response.TotalLaborCost); // 4h * 20
        Assert.Equal(WeekDayCalendarStatus.MissingResourcesOrUnpublished, response.CoverageStatus);

        var persisted = await context.ShiftEntities.ToListAsync();
        var saved = Assert.Single(persisted);
        Assert.Equal(7, saved.EmployeeId);
        Assert.Equal(ShiftStatus.Draft, saved.Status);
        Assert.Equal(10, saved.ShiftTemplateId);
    }

    [Fact]
    public async Task Handle_UnauthorizedEmployee_StripsToOpenRoleWithWarning()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        // No EmployeeSite link.

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var block = Assert.Single(result.Value!.Blocks);
        Assert.Null(block.EmployeeId);
        Assert.True(block.IsOpenRole);
        Assert.True(block.Stripped);
        Assert.Equal(ApplyTemplateStripCodes.SiteUnauthorized, block.StripCode);
        Assert.Equal(2, block.JobRoleId); // role retained

        var warning = Assert.Single(result.Value.Warnings);
        Assert.Equal(7, warning.EmployeeId);
        Assert.Equal("John Doe", warning.EmployeeName);
        Assert.Equal("Cashier", warning.Role);
    }

    [Fact]
    public async Task Handle_ApprovedLeave_StripsWithOnLeaveCode()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        SeedRequestType(context);
        context.Requests.Add(new Request
        {
            EmployeeId = 7,
            RequestTypeId = 1,
            StartDate = new DateTime(2026, 9, 10),
            EndDate = new DateTime(2026, 9, 10),
            Status = "Approved"
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.Null(block.EmployeeId);
        Assert.Equal(ApplyTemplateStripCodes.EmployeeOnLeave, block.StripCode);
        Assert.Contains("Annual Leave", result.Value.Warnings.Single().Reason);
    }

    [Fact]
    public async Task Handle_RemoteWorkRequest_StripsWithRemoteWorkCode()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        SeedRequestType(context);
        context.Requests.Add(new Request
        {
            EmployeeId = 7,
            RequestTypeId = 1,
            StartDate = new DateTime(2026, 9, 9),
            EndDate = new DateTime(2026, 9, 11),
            Status = "remote work"
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.Null(block.EmployeeId);
        Assert.Equal(ApplyTemplateStripCodes.EmployeeRemoteWork, block.StripCode);
    }

    [Fact]
    public async Task Handle_PendingLeave_DoesNotStrip()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        SeedRequestType(context);
        context.Requests.Add(new Request
        {
            EmployeeId = 7,
            RequestTypeId = 1,
            StartDate = new DateTime(2026, 9, 10),
            EndDate = new DateTime(2026, 9, 10),
            Status = "Pending"
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.Equal(7, block.EmployeeId);
        Assert.Empty(result.Value.Warnings);
    }

    [Fact]
    public async Task Handle_OfflineWorkday_StripsWithAvailabilityCode()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context, offlineJson: "[\"Thursday\"]");
        SeedTemplate(context);
        Authorize(context, 7);

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.Null(block.EmployeeId);
        Assert.Equal(ApplyTemplateStripCodes.OutsideEmployeeAvailability, block.StripCode);
    }

    [Fact]
    public async Task Handle_OnlineWorkday_KeepsAssignment()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context, onlineJson: "[\"Thursday\"]");
        SeedTemplate(context);
        Authorize(context, 7);

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        Assert.Equal(7, Assert.Single(result.Value!.Blocks).EmployeeId);
    }

    [Fact]
    public async Task Handle_QualificationMismatch_Strips()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        context.JobRoles.Add(new JobRole { Id = 5, Title = "Barista" });
        context.SaveChanges();
        SeedEmployee(context, jobRoleId: 5);
        SeedTemplate(context, jobRoleId: 2);
        Authorize(context, 7);

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.Null(block.EmployeeId);
        Assert.Equal(ApplyTemplateStripCodes.QualificationMismatch, block.StripCode);
        Assert.Equal(2, block.JobRoleId);
    }

    [Fact]
    public async Task Handle_OverlappingEmployeeShift_SetsCollisionWithoutFailing()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        context.ShiftEntities.Add(new ShiftEntity
        {
            SiteId = 3,
            JobRoleId = 2,
            EmployeeId = 7,
            StartTime = new DateTimeOffset(2026, 9, 10, 11, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 10, 15, 0, 0, TimeSpan.Zero),
            IsPublished = true
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var block = Assert.Single(result.Value!.Blocks);
        Assert.Equal(7, block.EmployeeId);
        Assert.True(block.Collision);
        Assert.Equal(ApplyTemplateCollisionTypes.EmployeeOverlap, block.CollisionType);
        Assert.Equal(2, await context.ShiftEntities.CountAsync()); // nothing deleted
    }

    [Fact]
    public async Task Handle_KeepNew_DeletesOverlappingExisting()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        var existing = new ShiftEntity
        {
            SiteId = 3,
            JobRoleId = 2,
            EmployeeId = 7,
            StartTime = new DateTimeOffset(2026, 9, 10, 11, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 10, 15, 0, 0, TimeSpan.Zero),
            IsPublished = true
        };
        context.ShiftEntities.Add(existing);
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10, "new"), CancellationToken.None);

        Assert.True(result.Value!.Blocks.Single().Collision);
        Assert.DoesNotContain(await context.ShiftEntities.Select(s => s.Id).ToListAsync(), id => id == existing.Id);
    }

    [Fact]
    public async Task Handle_KeepExisting_PrunesCollidingNew()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        context.ShiftEntities.Add(new ShiftEntity
        {
            SiteId = 3,
            JobRoleId = 2,
            EmployeeId = 7,
            StartTime = new DateTimeOffset(2026, 9, 10, 11, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 10, 15, 0, 0, TimeSpan.Zero),
            IsPublished = true
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10, "existing"), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.True(block.Pruned);
        Assert.Equal(0, block.ShiftId);
        Assert.Equal(1, result.Value.PrunedCount);
        Assert.Equal(1, await context.ShiftEntities.CountAsync()); // existing kept, new not persisted
    }

    [Fact]
    public async Task Handle_ClosedSiteDay_StripsWithSiteClosedCode()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        context.SiteOperationalHours.Add(new SiteOperationalHour
        {
            SiteId = 3,
            DayOfWeek = DayOfWeek.Thursday,
            IsOpen = false,
            OpenTime = new TimeOnly(9, 0),
            CloseTime = new TimeOnly(17, 0)
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.Null(block.EmployeeId);
        Assert.Equal(ApplyTemplateStripCodes.SiteClosed, block.StripCode);
        Assert.Equal(WeekDayCalendarStatus.DimmedDayOff, result.Value.CoverageStatus);
    }

    [Fact]
    public async Task Handle_OvertimePricing_SplitsRegularAndOvertime()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context, overtimeThreshold: 40m, overtimeRate: 30m);
        SeedTemplate(context);
        Authorize(context, 7);
        // 38h earlier in the same week (Mon Sep 7 - Wed Sep 9).
        context.ShiftEntities.AddRange(
            new ShiftEntity
            {
                SiteId = 3, JobRoleId = 2, EmployeeId = 7,
                StartTime = new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 9, 7, 19, 0, 0, TimeSpan.Zero),
                IsPublished = true
            },
            new ShiftEntity
            {
                SiteId = 3, JobRoleId = 2, EmployeeId = 7,
                StartTime = new DateTimeOffset(2026, 9, 8, 9, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 9, 8, 19, 0, 0, TimeSpan.Zero),
                IsPublished = true
            },
            new ShiftEntity
            {
                SiteId = 3, JobRoleId = 2, EmployeeId = 7,
                StartTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 9, 9, 19, 0, 0, TimeSpan.Zero),
                IsPublished = true
            },
            new ShiftEntity
            {
                SiteId = 3, JobRoleId = 2, EmployeeId = 7,
                StartTime = new DateTimeOffset(2026, 9, 9, 1, 0, 0, TimeSpan.Zero),
                EndTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
                IsPublished = true
            });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        // 38h before + 4h block: 2h regular x 20 + 2h overtime x 30.
        Assert.Equal(100m, result.Value!.TotalLaborCost);
        Assert.Equal(40m, result.Value.RegularCost);
        Assert.Equal(60m, result.Value.OvertimeCost);
    }

    [Fact]
    public async Task Handle_OvertimeRateFallback_UsesOnePointFiveMultiplier()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context, overtimeThreshold: 4m, overtimeRate: 0m);
        SeedTemplate(context);
        Authorize(context, 7);
        context.ShiftEntities.Add(new ShiftEntity
        {
            SiteId = 3, JobRoleId = 2, EmployeeId = 7,
            StartTime = new DateTimeOffset(2026, 9, 9, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 9, 13, 0, 0, TimeSpan.Zero),
            IsPublished = true
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        // Threshold already met: 4h x (20 x 1.5) = 120.
        Assert.Equal(120m, result.Value!.TotalLaborCost);
        Assert.Equal(0m, result.Value.RegularCost);
        Assert.Equal(120m, result.Value.OvertimeCost);
    }

    [Fact]
    public async Task Handle_CrossMidnightExistingShift_DetectedAsCollision()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        // Starts the previous day, overlaps the new 09:00-13:00 block until 10:00.
        context.ShiftEntities.Add(new ShiftEntity
        {
            SiteId = 3, JobRoleId = 2, EmployeeId = 7,
            StartTime = new DateTimeOffset(2026, 9, 9, 22, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero),
            IsPublished = true
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.True(block.Collision);
        Assert.Equal(ApplyTemplateCollisionTypes.EmployeeOverlap, block.CollisionType);
    }

    [Fact]
    public async Task Handle_NonOverlappingExistingShift_NoFlags()
    {
        using var context = CreateDbContext();
        SeedSite(context);
        SeedEmployee(context);
        SeedTemplate(context);
        Authorize(context, 7);
        context.ShiftEntities.Add(new ShiftEntity
        {
            SiteId = 3, JobRoleId = 2, EmployeeId = 7,
            StartTime = new DateTimeOffset(2026, 9, 10, 14, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 10, 18, 0, 0, TimeSpan.Zero),
            IsPublished = true
        });
        context.SaveChanges();

        var result = await CreateHandler(context).Handle(
            new ApplyTemplateCommand(3, TargetDate, 10), CancellationToken.None);

        var block = Assert.Single(result.Value!.Blocks);
        Assert.False(block.Collision);
        Assert.False(block.Conflict);
        // Both shifts priced at the regular rate: 4h + 4h x 20.
        Assert.Equal(160m, result.Value.TotalLaborCost);
        Assert.Equal(0m, result.Value.OvertimeCost);
    }
}
