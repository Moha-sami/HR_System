using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Points.Automation.Evaluators;

public interface IAutomationEvaluator
{
    AutomationCategory Category { get; }
    Task<(int TotalPoints, List<RuleEvaluationDetailDto> RuleEvaluations)> EvaluateAsync(
        EvaluationContext context,
        List<PointsAutomationSetting> settings,
        CancellationToken cancellationToken);
}

public class EvaluationContext
{
    public int EmployeeId { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
    public List<EmployeeTask> Tasks { get; set; } = new();
    public List<PerformanceSubmission> Submissions { get; set; } = new();
}