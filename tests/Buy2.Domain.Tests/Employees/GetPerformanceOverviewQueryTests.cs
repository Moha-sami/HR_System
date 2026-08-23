using Buy2.Application.Features.Employees.GetPerformanceOverview;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Employees;

public class GetPerformanceOverviewQueryTests
{
    private Buy2DbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenEmployeeIsDeletedOrNotFound()
    {
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var deletedEmp = new Employee
            {
                FirstName = "Deleted",
                LastName = "User",
                Email = "deleted@buy2.com",
                IsDeleted = true
            };
            context.Employees.Add(deletedEmp);
            await context.SaveChangesAsync();

            var empRepo = new GenericRepository<Employee>(context);
            var subRepo = new GenericRepository<PerformanceSubmission>(context);
            var taskRepo = new GenericRepository<EmployeeTask>(context);
            var achRepo = new GenericRepository<EmployeeAchievement>(context);

            var handler = new GetPerformanceOverviewQueryHandler(empRepo, subRepo, taskRepo, achRepo);

            // Act & Assert
            var resultNotFound = await handler.Handle(new GetPerformanceOverviewQuery(999), CancellationToken.None);
            Assert.Null(resultNotFound);

            var resultDeleted = await handler.Handle(new GetPerformanceOverviewQuery(deletedEmp.Id), CancellationToken.None);
            Assert.Null(resultDeleted);
        }
    }

    [Fact]
    public async Task Handle_MapsRichBadgeProperties_WhenBadgeNavigationIsPresent()
    {
        var dbName = Guid.NewGuid().ToString();
        int empId;

        using (var writeContext = CreateDbContext(dbName))
        {
            var emp = new Employee
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@buy2.com",
                IsDeleted = false
            };
            writeContext.Employees.Add(emp);
            await writeContext.SaveChangesAsync();
            empId = emp.Id;

            var badge = new Badge
            {
                Name = "Top Performer 2026",
                Description = "Awarded for exceptional quarter score",
                IconUrl = "https://cdn.buy2.com/badges/top_performer.png",
                TriggerType = "QuarterlyScore",
                TriggerThreshold = 95.0m,
                PointsAwarded = 500
            };
            writeContext.Badges.Add(badge);
            await writeContext.SaveChangesAsync();

            var achievementWithBadge = new EmployeeAchievement
            {
                EmployeeId = empId,
                BadgeId = badge.Id,
                Badge = badge,
                BadgeType = "Performance",
                AwardedAt = DateTime.UtcNow.AddDays(-5),
                PointsAwarded = 500
            };

            var legacyAchievement = new EmployeeAchievement
            {
                EmployeeId = empId,
                BadgeId = null,
                Badge = null,
                BadgeType = "Attendance Champion",
                AwardedAt = DateTime.UtcNow.AddDays(-10),
                PointsAwarded = 100
            };

            writeContext.EmployeeAchievements.AddRange(achievementWithBadge, legacyAchievement);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateDbContext(dbName);
        var empRepo = new GenericRepository<Employee>(readContext);
        var subRepo = new GenericRepository<PerformanceSubmission>(readContext);
        var taskRepo = new GenericRepository<EmployeeTask>(readContext);
        var achRepo = new GenericRepository<EmployeeAchievement>(readContext);

        var handler = new GetPerformanceOverviewQueryHandler(empRepo, subRepo, taskRepo, achRepo);

        // Act
        var result = await handler.Handle(new GetPerformanceOverviewQuery(empId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Achievements.Count);

        var enrichedBadge = result.Achievements.FirstOrDefault(a => a.Title == "Top Performer 2026");
        Assert.NotNull(enrichedBadge);
        Assert.Equal("Awarded for exceptional quarter score", enrichedBadge.Description);
        Assert.Equal("https://cdn.buy2.com/badges/top_performer.png", enrichedBadge.IconUrl);
        Assert.Equal(500, enrichedBadge.PointsAwarded);

        var legacyBadge = result.Achievements.FirstOrDefault(a => a.Title == "Attendance Champion");
        Assert.NotNull(legacyBadge);
        Assert.Equal("Awarded badge: Attendance Champion", legacyBadge.Description);
        Assert.Null(legacyBadge.IconUrl);
        Assert.Equal(100, legacyBadge.PointsAwarded);
    }
}
