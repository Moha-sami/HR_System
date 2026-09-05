using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Features.Points.ExecuteAutomationJob.Evaluators;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Points.ExecuteAutomationJob.Evaluators;

public class TaskAutomationEvaluator : IAutomationEvaluator
{
    public AutomationCategory Category => AutomationCategory.Tasks;

    public Task<(int TotalPoints, List<RuleEvaluationDetailDto> RuleEvaluations)> EvaluateAsync(
        EvaluationContext context,
        List<PointsAutomationSetting> settings,
        CancellationToken cancellationToken)
    {
        var totalPoints = 0;
        var ruleEvaluations = new List<RuleEvaluationDetailDto>();

        var tasks = context.Tasks;

        var completionSettings = settings.Where(s => s.SubCategory.Equals("Completion", StringComparison.OrdinalIgnoreCase)).ToList();
        if (completionSettings.Any() && tasks.Any())
        {
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == EmployeeTaskStatus.Completed);
            var completionRate = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0;

            foreach (var setting in completionSettings)
            {
                var matchedRange = setting.Ranges.FirstOrDefault(r =>
                    (r.FromValue == null || completionRate >= r.FromValue.Value) &&
                    (r.ToValue == null || completionRate <= r.ToValue.Value));

                if (matchedRange != null && matchedRange.PointsValue != 0)
                {
                    totalPoints += matchedRange.PointsValue;
                    ruleEvaluations.Add(new RuleEvaluationDetailDto(
                        RuleType: "Task Completion Rate",
                        MetricEvaluated: "CompletionRate",
                        CalculatedValue: completionRate,
                        MatchedRange: $"{matchedRange.FromValue} - {matchedRange.ToValue}",
                        RewardOrDeduction: matchedRange.PointsValue,
                        TaskPriority: null,
                        OverdueDays: null,
                        Source: "Tasks"
                    ));
                }
            }
        }

        var deadlineSettings = settings.Where(s => s.SubCategory.Equals("Deadline", StringComparison.OrdinalIgnoreCase)).ToList();
        if (deadlineSettings.Any())
        {
            var overdueTasks = tasks.Where(t => t.Status == EmployeeTaskStatus.Overdue
                && t.DueDate.HasValue
                && t.DueDate.Value.Date < context.PeriodEnd.Date).ToList();

            if (overdueTasks.Any())
            {
                var totalPenalty = 0;
                foreach (var task in overdueTasks)
                {
                    var daysOverdue = (context.PeriodEnd.Date - task.DueDate!.Value.Date).Days;
                    var priority = task.Priority ?? "Medium";

                    foreach (var setting in deadlineSettings)
                    {
                        var matchedRange = setting.Ranges
                            .Where(r => r.TaskPriority == null || r.TaskPriority.Equals(priority, StringComparison.OrdinalIgnoreCase))
                            .FirstOrDefault(r =>
                                (r.FromValue == null || daysOverdue >= r.FromValue.Value) &&
                                (r.ToValue == null || daysOverdue <= r.ToValue.Value));

                        if (matchedRange != null && matchedRange.PointsValue != 0)
                        {
                            var taskPenalty = matchedRange.PointsValue * daysOverdue;
                            totalPenalty += taskPenalty;

                            ruleEvaluations.Add(new RuleEvaluationDetailDto(
                                RuleType: "Overdue Task Penalty",
                                MetricEvaluated: "DaysOverdue",
                                CalculatedValue: daysOverdue,
                                MatchedRange: $"{matchedRange.FromValue} - {matchedRange.ToValue}",
                                RewardOrDeduction: -taskPenalty,
                                TaskPriority: priority,
                                OverdueDays: daysOverdue,
                                Source: "Tasks"
                            ));
                            break;
                        }
                    }
                }

                totalPoints -= totalPenalty;
            }
        }

        return Task.FromResult((totalPoints, ruleEvaluations));
    }
}