using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Features.Points.ExecuteAutomationJob.Evaluators;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Points.ExecuteAutomationJob.Evaluators;

public class PerformanceAutomationEvaluator : IAutomationEvaluator
{
    public AutomationCategory Category => AutomationCategory.Performance;

    public Task<(int TotalPoints, List<RuleEvaluationDetailDto> RuleEvaluations)> EvaluateAsync(
        EvaluationContext context,
        List<PointsAutomationSetting> settings,
        CancellationToken cancellationToken)
    {
        var totalPoints = 0;
        var ruleEvaluations = new List<RuleEvaluationDetailDto>();

        var submissions = context.Submissions;

        foreach (var submission in submissions)
        {
            var metricName = submission.PerformanceMetric?.Name ?? "Unknown";
            var achievedPercent = Math.Clamp(submission.AchievedPercent, 0, 100);

            var matchingSettings = settings.Where(s => 
                s.MetricId.HasValue && s.MetricId.Value == submission.MetricId).ToList();

            if (!matchingSettings.Any())
            {
                matchingSettings = settings.ToList();
            }

            foreach (var setting in matchingSettings)
            {
                var matchedRange = setting.Ranges.FirstOrDefault(r =>
                    (r.FromValue == null || achievedPercent >= r.FromValue.Value) &&
                    (r.ToValue == null || achievedPercent <= r.ToValue.Value));

                if (matchedRange != null && matchedRange.PointsValue != 0)
                {
                    totalPoints += matchedRange.PointsValue;
                    ruleEvaluations.Add(new RuleEvaluationDetailDto(
                        RuleType: "Performance Metric",
                        MetricEvaluated: metricName,
                        CalculatedValue: achievedPercent,
                        MatchedRange: $"{matchedRange.FromValue} - {matchedRange.ToValue}",
                        RewardOrDeduction: matchedRange.PointsValue,
                        TaskPriority: null,
                        OverdueDays: null,
                        Source: "Performance"
                    ));
                }
            }
        }

        return Task.FromResult((totalPoints, ruleEvaluations));
    }
}