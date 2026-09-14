using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Features.Points.Automation.SaveAutomationSettings;
using Buy2.Domain.Entities;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Buy2.Domain.Tests.Points;

public class SaveAutomationSettingsHandlerTests
{
    private static Buy2DbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new Buy2DbContext(options);
    }

    private static SaveAutomationSettingsCommandHandler CreateHandler(Buy2DbContext context)
    {
        return new SaveAutomationSettingsCommandHandler(
            new GenericRepository<PointsAutomationSetting>(context),
            new GenericRepository<PointsAutomationRange>(context),
            new UnitOfWork(context));
    }

    private static SaveAutomationSettingsCommand BuildCommand(
        string rangeType, int pointsValue, bool isEnabled, string subCategory = "Score", string category = "Performance")
    {
        return new SaveAutomationSettingsCommand(new SaveAutomationSettingsDto(
            "Monthly",
            new List<AutomationSettingCategoryDto>
            {
                new AutomationSettingCategoryDto(
                    0, category, subCategory, isEnabled,
                    new List<AutomationRangeDto>
                    {
                        new AutomationRangeDto(null, rangeType, 0m, 100m, subCategory == "Deadline" ? "High" : null, pointsValue)
                    })
            }));
    }

    [Fact]
    public async Task Handler_DeductionPositive_NormalizedToNegative()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        // Frontend sends Deduction=500 positive -> auto-fixed to -500.
        var result = await handler.Handle(BuildCommand("Deduction", 500, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await context.Set<PointsAutomationRange>().SingleAsync();
        Assert.Equal(-500, stored.PointsValue);
    }

    [Fact]
    public async Task Handler_RewardNegative_NormalizedToPositive()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(BuildCommand("Reward", -200, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await context.Set<PointsAutomationRange>().SingleAsync();
        Assert.Equal(200, stored.PointsValue);
    }

    [Fact]
    public async Task Handler_DisabledWithGarbageRanges_StoredAsEmpty()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var cmd = new SaveAutomationSettingsCommand(new SaveAutomationSettingsDto(
            "Monthly",
            new List<AutomationSettingCategoryDto>
            {
                new AutomationSettingCategoryDto(
                    0, "TimeAndAttendance", "Lateness", false,
                    new List<AutomationRangeDto>
                    {
                        new AutomationRangeDto(null, "Garbage", 80m, 10m, null, 0),
                        new AutomationRangeDto(null, "Reward", 70m, 100m, null, -50)
                    })
            }));
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var setting = await context.Set<PointsAutomationSetting>().Include(s => s.Ranges).SingleAsync();
        Assert.False(setting.IsEnabled);
        Assert.Empty(setting.Ranges);
    }

    [Fact]
    public async Task Handler_DisabledUpdate_DeletesOldRanges()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var seed = await handler.Handle(BuildCommand("Reward", 100, true, "AttendanceRate", "TimeAndAttendance"), CancellationToken.None);
        Assert.True(seed.IsSuccess);
        Assert.Equal(1, await context.Set<PointsAutomationRange>().CountAsync());

        var disable = await handler.Handle(
            new SaveAutomationSettingsCommand(new SaveAutomationSettingsDto(
                "Monthly",
                new List<AutomationSettingCategoryDto>
                {
                    new AutomationSettingCategoryDto(0, "TimeAndAttendance", "AttendanceRate", false, new List<AutomationRangeDto>())
                })),
            CancellationToken.None);

        Assert.True(disable.IsSuccess);
        Assert.Equal(0, await context.Set<PointsAutomationRange>().CountAsync());
    }

    [Fact]
    public async Task Handler_ZeroPointsValue_Rejected()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(BuildCommand("Reward", 0, true), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handler_UnknownRangeType_Rejected()
    {
        using var context = CreateDbContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(BuildCommand("Bonus", 100, true), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
