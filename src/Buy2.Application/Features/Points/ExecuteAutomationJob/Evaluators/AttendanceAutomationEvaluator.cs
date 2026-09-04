using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Points.ExecuteAutomationJob.Evaluators;

public class AttendanceAutomationEvaluator : IAutomationEvaluator
{
    public AutomationCategory Category => AutomationCategory.TimeAndAttendance;

    public Task<(int TotalPoints, List<RuleEvaluationDetailDto> RuleEvaluations)> EvaluateAsync(
        EvaluationContext context,
        List<PointsAutomationSetting> settings,
        CancellationToken cancellationToken)
    {
        var totalPoints = 0;
        var ruleEvaluations = new List<RuleEvaluationDetailDto>();

        var attendanceRecords = context.AttendanceRecords;

        var workingDays = attendanceRecords
            .Where(r =>
                r.Status != AttendanceDayStatus.PublicHoliday &&
                r.Status != AttendanceDayStatus.AttendanceNotRequired)
            .ToList();

        if (!workingDays.Any())
        {
            return Task.FromResult((0, ruleEvaluations));
        }

        var attendanceRateSettings = settings
            .Where(s => s.SubCategory.Equals(
                "AttendanceRate",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (attendanceRateSettings.Any())
        {
            var presentDays = workingDays.Count(r =>
                r.Status == AttendanceDayStatus.Present ||
                r.Status == AttendanceDayStatus.OnTime);

            var attendanceRate =
                (decimal)presentDays / workingDays.Count * 100;

            foreach (var setting in attendanceRateSettings)
            {
                var matchedRange = setting.Ranges.FirstOrDefault(r =>
                    (r.FromValue == null ||
                     attendanceRate >= r.FromValue.Value) &&
                    (r.ToValue == null ||
                     attendanceRate <= r.ToValue.Value));

                if (matchedRange != null && matchedRange.PointsValue != 0)
                {
                    totalPoints += matchedRange.PointsValue;

                    ruleEvaluations.Add(new RuleEvaluationDetailDto(
                        RuleType: "Attendance Rate",
                        MetricEvaluated: "AttendanceRate",
                        CalculatedValue: attendanceRate,
                        MatchedRange:
                            $"{matchedRange.FromValue} - {matchedRange.ToValue}",
                        RewardOrDeduction: matchedRange.PointsValue,
                        TaskPriority: null,
                        OverdueDays: null,
                        Source: "Attendance"
                    ));
                }
            }
        }

        var latenessSettings = settings
            .Where(s => s.SubCategory.Equals(
                "Lateness",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (latenessSettings.Any())
        {
            var lateRecords = workingDays
                .Where(r =>
                    r.LatenessMinutes.HasValue &&
                    r.LatenessMinutes.Value > 0)
                .ToList();

            var totalLatenessMinutes = lateRecords
                .Sum(r => r.LatenessMinutes!.Value);

            foreach (var setting in latenessSettings)
            {
                var matchedRange = setting.Ranges.FirstOrDefault(r =>
                    (r.FromValue == null ||
                     totalLatenessMinutes >= r.FromValue.Value) &&
                    (r.ToValue == null ||
                     totalLatenessMinutes <= r.ToValue.Value));

                if (matchedRange != null && matchedRange.PointsValue != 0)
                {
                    totalPoints += matchedRange.PointsValue;

                    ruleEvaluations.Add(new RuleEvaluationDetailDto(
                        RuleType: "Daily Lateness",
                        MetricEvaluated: "TotalLatenessMinutes",
                        CalculatedValue: totalLatenessMinutes,
                        MatchedRange:
                            $"{matchedRange.FromValue} - {matchedRange.ToValue}",
                        RewardOrDeduction: matchedRange.PointsValue,
                        TaskPriority: null,
                        OverdueDays: null,
                        Source: "Attendance"
                    ));
                }
            }
        }

        var consecutiveSettings = settings
            .Where(s => s.SubCategory.Equals(
                "ConsecutiveOnTime",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (consecutiveSettings.Any())
        {
            var consecutiveOnTime =
                CalculateConsecutiveOnTime(workingDays);

            foreach (var setting in consecutiveSettings)
            {
                var matchedRange = setting.Ranges.FirstOrDefault(r =>
                    (r.FromValue == null ||
                     consecutiveOnTime >= r.FromValue.Value) &&
                    (r.ToValue == null ||
                     consecutiveOnTime <= r.ToValue.Value));

                if (matchedRange != null && matchedRange.PointsValue != 0)
                {
                    totalPoints += matchedRange.PointsValue;

                    ruleEvaluations.Add(new RuleEvaluationDetailDto(
                        RuleType: "Consecutive On-Time Attendance",
                        MetricEvaluated: "ConsecutiveOnTimeDays",
                        CalculatedValue: consecutiveOnTime,
                        MatchedRange:
                            $"{matchedRange.FromValue} - {matchedRange.ToValue}",
                        RewardOrDeduction: matchedRange.PointsValue,
                        TaskPriority: null,
                        OverdueDays: null,
                        Source: "Attendance"
                    ));
                }
            }
        }

        return Task.FromResult((totalPoints, ruleEvaluations));
    }

    private static int CalculateConsecutiveOnTime(
        List<AttendanceRecord> workingDays)
    {
        var sortedDays = workingDays
            .OrderBy(d => d.Date)
            .ToList();

        var maxConsecutive = 0;
        var currentConsecutive = 0;

        foreach (var day in sortedDays)
        {
            
            var isOnTime =
                (day.Status == AttendanceDayStatus.Present ||
                 day.Status == AttendanceDayStatus.OnTime) &&
                (!day.LatenessMinutes.HasValue ||
                 day.LatenessMinutes.Value == 0);

            if (isOnTime)
            {
                currentConsecutive++;

                maxConsecutive = Math.Max(
                    maxConsecutive,
                    currentConsecutive);
            }
            else
            {
                currentConsecutive = 0;
            }
        }

        return maxConsecutive;
    }
}
