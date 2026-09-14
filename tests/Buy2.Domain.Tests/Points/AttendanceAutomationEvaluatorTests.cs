using Buy2.Application.Features.Points.Automation.Evaluators;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Xunit;

namespace Buy2.Domain.Tests.Points;

public class AttendanceAutomationEvaluatorTests
{
    private readonly AttendanceAutomationEvaluator _sut = new();

    private static List<AttendanceRecord> BuildRecords(int onTimeDays, int lateDays)
    {
        var records = new List<AttendanceRecord>();
        var date = new DateTime(2026, 1, 1);
        for (var i = 0; i < onTimeDays; i++)
            records.Add(new AttendanceRecord { EmployeeId = 1, Date = date.AddDays(i), Status = AttendanceDayStatus.OnTime });
        for (var i = 0; i < lateDays; i++)
            records.Add(new AttendanceRecord
            {
                EmployeeId = 1,
                Date = date.AddDays(onTimeDays + i),
                Status = AttendanceDayStatus.Late,
                LatenessMinutes = 15
            });
        return records;
    }

    private static PointsAutomationSetting RateSetting(int points = 100)
    {
        return new PointsAutomationSetting
        {
            Category = AutomationCategory.TimeAndAttendance,
            SubCategory = "AttendanceRate",
            IsEnabled = true,
            Ranges = new List<PointsAutomationRange>
            {
                new PointsAutomationRange { RangeType = "Reward", FromValue = 0, ToValue = 100, PointsValue = points }
            }
        };
    }

    private static EvaluationContext Ctx(List<AttendanceRecord> records)
    {
        return new EvaluationContext
        {
            EmployeeId = 1,
            PeriodStart = new DateTimeOffset(new DateTime(2026, 1, 1)),
            PeriodEnd = new DateTimeOffset(new DateTime(2026, 1, 31)),
            AttendanceRecords = records
        };
    }

    [Fact]
    public async Task AttendanceRate_LateCountsAsPresent()
    {
        // 20 working days, 2 Late -> must be 100% (not 90%).
        var records = BuildRecords(onTimeDays: 18, lateDays: 2);
        var (points, evals) = await _sut.EvaluateAsync(Ctx(records), new List<PointsAutomationSetting> { RateSetting() }, CancellationToken.None);

        Assert.Equal(100, points);
        Assert.Single(evals);
        Assert.Equal(100m, evals[0].CalculatedValue);
    }

    [Fact]
    public async Task AttendanceRate_PresentWithLatenessMinutesCountsAsPresent()
    {
        var records = new List<AttendanceRecord>
        {
            new AttendanceRecord { EmployeeId = 1, Date = new DateTime(2026, 1, 1), Status = AttendanceDayStatus.Present, LatenessMinutes = 30 },
            new AttendanceRecord { EmployeeId = 1, Date = new DateTime(2026, 1, 2), Status = AttendanceDayStatus.OnTime }
        };
        var (points, evals) = await _sut.EvaluateAsync(Ctx(records), new List<PointsAutomationSetting> { RateSetting() }, CancellationToken.None);

        Assert.Equal(100, points);
        Assert.Equal(100m, evals[0].CalculatedValue);
    }

    [Fact]
    public async Task Lateness_StillDeducts()
    {
        var records = BuildRecords(onTimeDays: 18, lateDays: 2); // 30 total lateness minutes
        var latenessSetting = new PointsAutomationSetting
        {
            Category = AutomationCategory.TimeAndAttendance,
            SubCategory = "Lateness",
            IsEnabled = true,
            Ranges = new List<PointsAutomationRange>
            {
                new PointsAutomationRange { RangeType = "Deduction", FromValue = 0, ToValue = 100, PointsValue = -10 }
            }
        };
        var (points, evals) = await _sut.EvaluateAsync(Ctx(records), new List<PointsAutomationSetting> { latenessSetting }, CancellationToken.None);

        Assert.Equal(-10, points);
        Assert.Single(evals);
        Assert.Equal(30m, evals[0].CalculatedValue);
    }

    [Fact]
    public async Task ConsecutiveOnTime_StillExcludesLate()
    {
        var records = new List<AttendanceRecord>
        {
            new AttendanceRecord { EmployeeId = 1, Date = new DateTime(2026, 1, 1), Status = AttendanceDayStatus.OnTime },
            new AttendanceRecord { EmployeeId = 1, Date = new DateTime(2026, 1, 2), Status = AttendanceDayStatus.Late, LatenessMinutes = 10 },
            new AttendanceRecord { EmployeeId = 1, Date = new DateTime(2026, 1, 3), Status = AttendanceDayStatus.OnTime }
        };
        var consecutiveSetting = new PointsAutomationSetting
        {
            Category = AutomationCategory.TimeAndAttendance,
            SubCategory = "ConsecutiveOnTime",
            IsEnabled = true,
            Ranges = new List<PointsAutomationRange>
            {
                new PointsAutomationRange { RangeType = "Reward", FromValue = 0, ToValue = 100, PointsValue = 5 }
            }
        };
        var (_, evals) = await _sut.EvaluateAsync(Ctx(records), new List<PointsAutomationSetting> { consecutiveSetting }, CancellationToken.None);

        Assert.Single(evals);
        Assert.Equal(1m, evals[0].CalculatedValue);
    }

    [Fact]
    public async Task Deadline_NegativeValueDeductsWithoutSignFlip()
    {
        var sut = new TaskAutomationEvaluator();
        var ctx = new EvaluationContext
        {
            EmployeeId = 1,
            PeriodStart = new DateTimeOffset(new DateTime(2026, 1, 1)),
            PeriodEnd = new DateTimeOffset(new DateTime(2026, 1, 10)),
            Tasks = new List<EmployeeTask>
            {
                new EmployeeTask
                {
                    EmployeeId = 1,
                    TaskName = "T1",
                    Status = EmployeeTaskStatus.Overdue,
                    DueDate = new DateTime(2026, 1, 8),
                    Priority = "High"
                }
            }
        };
        var settings = new List<PointsAutomationSetting>
        {
            new PointsAutomationSetting
            {
                Category = AutomationCategory.Tasks,
                SubCategory = "Deadline",
                IsEnabled = true,
                Ranges = new List<PointsAutomationRange>
                {
                    new PointsAutomationRange { RangeType = "Deduction", FromValue = 0, ToValue = 100, TaskPriority = "High", PointsValue = -30 }
                }
            }
        };

        var (points, evals) = await sut.EvaluateAsync(ctx, settings, CancellationToken.None);

        // 2 days overdue * -30 = -60 deduction (must NOT flip to +60).
        Assert.Equal(-60, points);
        Assert.Single(evals);
        Assert.Equal(-60, evals[0].RewardOrDeduction);
    }
}
