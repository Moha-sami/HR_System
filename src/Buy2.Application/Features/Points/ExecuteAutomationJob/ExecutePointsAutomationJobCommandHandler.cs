using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Features.Points.ExecuteAutomationJob;
using Buy2.Application.Features.Points.ExecuteAutomationJob.Evaluators;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Points.ExecuteAutomationJob;

public class ExecutePointsAutomationJobCommandHandler : IRequestHandler<ExecutePointsAutomationJobCommand, ExecutePointsAutomationJobResult>
{
    private readonly IRepository<PointsAutomationSetting> _automationSettingRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRecordRepository;
    private readonly IRepository<EmployeeTask> _employeeTaskRepository;
    private readonly IRepository<PerformanceSubmission> _performanceSubmissionRepository;
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IAutomationEvaluator> _evaluators;

    public ExecutePointsAutomationJobCommandHandler(
        IRepository<PointsAutomationSetting> automationSettingRepository,
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceRecord> attendanceRecordRepository,
        IRepository<EmployeeTask> employeeTaskRepository,
        IRepository<PerformanceSubmission> performanceSubmissionRepository,
        IRepository<PointsTransaction> pointsTransactionRepository,
        IUnitOfWork unitOfWork,
        IEnumerable<IAutomationEvaluator> evaluators)
    {
        _automationSettingRepository = automationSettingRepository;
        _employeeRepository = employeeRepository;
        _attendanceRecordRepository = attendanceRecordRepository;
        _employeeTaskRepository = employeeTaskRepository;
        _performanceSubmissionRepository = performanceSubmissionRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _unitOfWork = unitOfWork;
        _evaluators = evaluators;
    }

    public async Task<ExecutePointsAutomationJobResult> Handle(ExecutePointsAutomationJobCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;
        var executionId = Guid.NewGuid().ToString();
        var executedAt = DateTimeOffset.UtcNow;

        var enabledSettings = await _automationSettingRepository.Query()
            .AsNoTracking()
            .Include(s => s.Ranges)
            .Where(s => s.IsEnabled)
            .ToListAsync(cancellationToken);

        if (!enabledSettings.Any())
        {
            return new ExecutePointsAutomationJobResult(
                IsSuccess: false,
                ErrorMessage: "No enabled automation settings found.",
                IsNotFound: true);
        }

        var validationResult = ValidateRanges(enabledSettings);
        if (!validationResult.IsValid)
        {
            return new ExecutePointsAutomationJobResult(
                IsSuccess: false,
                ErrorMessage: validationResult.ErrorMessage,
                IsNotFound: false);
        }

        var automationPeriod = enabledSettings.First().AutomationPeriod;
        var (periodStart, periodEnd) = ResolveEvaluationPeriod(automationPeriod);

        var targetEmployees = await ResolveTargetEmployees(dto.TargetEmployeeIds, cancellationToken);
        if (!targetEmployees.Any())
        {
            return new ExecutePointsAutomationJobResult(
                IsSuccess: false,
                ErrorMessage: "No active employees found for evaluation.",
                IsNotFound: true);
        }

        var employeeIds = targetEmployees.Select(e => e.Id).ToList();

        var existingTransactions = await _pointsTransactionRepository.Query()
            .AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                && t.TriggeredBy == "KPI Achievement"
                && t.EvaluationPeriodStart == periodStart
                && t.EvaluationPeriodEnd == periodEnd)
            .Select(t => new { t.EmployeeId, t.AutomationCategory })
            .ToListAsync(cancellationToken);

        var existingTransactionKeys = existingTransactions
            .Select(t => (t.EmployeeId, t.AutomationCategory))
            .ToHashSet();

        var attendanceRecords = await _attendanceRecordRepository.Query()
            .AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId)
                && r.Date >= periodStart.Date
                && r.Date <= periodEnd.Date)
            .ToListAsync(cancellationToken);

        var employeeTasks = await _employeeTaskRepository.Query()
            .AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                && t.DueDate.HasValue
                && t.DueDate.Value.Date >= periodStart.Date
                && t.DueDate.Value.Date <= periodEnd.Date)
            .ToListAsync(cancellationToken);

        var performanceSubmissions = await _performanceSubmissionRepository.Query()
            .AsNoTracking()
            .Include(s => s.PerformanceMetric)
            .Where(s => employeeIds.Contains(s.EmployeeId)
                && s.SubmissionDate >= periodStart.Date
                && s.SubmissionDate <= periodEnd.Date)
            .ToListAsync(cancellationToken);

        var attendanceByEmployee = attendanceRecords.GroupBy(r => r.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
        var tasksByEmployee = employeeTasks.GroupBy(t => t.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());
        var submissionsByEmployee = performanceSubmissions.GroupBy(s => s.EmployeeId).ToDictionary(g => g.Key, g => g.ToList());

        var skippedEmployeeIds = new List<int>();
        var employeeFailures = new List<EmployeeAutomationFailureDto>();
        var successfulEvaluations = new List<EmployeeAutomationSuccessDto>();
        var totalPointsAwarded = 0;
        var totalPointsDeducted = 0;
        var transactionsCreated = 0;

        foreach (var employee in targetEmployees)
        {
            foreach (var evaluator in _evaluators)
            {
                var settings = enabledSettings.Where(s => s.Category == evaluator.Category).ToList();
                if (!settings.Any())
                {
                    continue;
                }

                var transactionKey = (employee.Id, evaluator.Category);
                if (existingTransactionKeys.Contains(transactionKey))
                {
                    if (!skippedEmployeeIds.Contains(employee.Id))
                    {
                        skippedEmployeeIds.Add(employee.Id);
                    }
                    continue;
                }

                try
                {
                    var context = new EvaluationContext
                    {
                        EmployeeId = employee.Id,
                        PeriodStart = periodStart,
                        PeriodEnd = periodEnd,
                        AttendanceRecords = attendanceByEmployee.GetValueOrDefault(employee.Id, new List<AttendanceRecord>()),
                        Tasks = tasksByEmployee.GetValueOrDefault(employee.Id, new List<EmployeeTask>()),
                        Submissions = submissionsByEmployee.GetValueOrDefault(employee.Id, new List<PerformanceSubmission>())
                    };

                    var (points, ruleEvaluations) = await evaluator.EvaluateAsync(context, settings, cancellationToken);

                    if (points != 0 && ruleEvaluations.Any())
                    {
                        var transactionType = points >= 0 ? TransactionType.Add : TransactionType.Deduct;

                        var transaction = new PointsTransaction
                        {
                            EmployeeId = employee.Id,
                            Amount = points,
                            TransactionType = transactionType,
                            TriggeredBy = "KPI Achievement",
                            Comments = $"{evaluator.Category} Automation",
                            EvaluationPeriodStart = periodStart,
                            EvaluationPeriodEnd = periodEnd,
                            AutomationCategory = evaluator.Category,
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        await _pointsTransactionRepository.AddAsync(transaction);

                        if (points > 0) totalPointsAwarded += points;
                        else totalPointsDeducted += Math.Abs(points);

                        successfulEvaluations.Add(new EmployeeAutomationSuccessDto(
                            EmployeeId: employee.Id,
                            Category: evaluator.Category.ToString(),
                            PointsAmount: points,
                            TransactionId: transaction.Id,
                            EvaluationPeriodStart: periodStart,
                            EvaluationPeriodEnd: periodEnd,
                            RuleEvaluations: ruleEvaluations
                        ));

                        transactionsCreated++;
                        existingTransactionKeys.Add(transactionKey);
                    }
                }
                catch (Exception ex)
                {
                    employeeFailures.Add(new EmployeeAutomationFailureDto(
                        EmployeeId: employee.Id,
                        Category: evaluator.Category.ToString(),
                        ErrorMessage: ex.Message
                    ));
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new AutomationJobResultDto(
            ExecutionId: executionId,
            ExecutedAt: executedAt,
            TotalEmployeesEvaluated: targetEmployees.Count,
            TotalPointsAwarded: totalPointsAwarded,
            TotalPointsDeducted: totalPointsDeducted,
            TransactionsCreatedCount: transactionsCreated,
            SkippedEmployeeIds: skippedEmployeeIds,
            EmployeeFailures: employeeFailures,
            SuccessfulEvaluations: successfulEvaluations,
            SummaryNotes: $"Automation executed for {targetEmployees.Count} employees across {_evaluators.Count()} categories. {skippedEmployeeIds.Count} skipped, {employeeFailures.Count} failures."
        );

        return new ExecutePointsAutomationJobResult(IsSuccess: true, Data: result);
    }

    private (DateTimeOffset, DateTimeOffset) ResolveEvaluationPeriod(AutomationPeriod period)
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.Date;

        switch (period)
        {
            case AutomationPeriod.Daily:
                return (today, today.AddDays(1).AddTicks(-1));
            case AutomationPeriod.Weekly:
            {
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                return (startOfWeek, startOfWeek.AddDays(7).AddTicks(-1));
            }
            case AutomationPeriod.BiWeekly:
            {
                var epoch = new DateTime(2000, 1, 1);
                var daysSinceEpoch = (today - epoch).Days;
                var biWeekNumber = daysSinceEpoch / 14;
                var biWeekStart = epoch.AddDays(biWeekNumber * 14);
                return (biWeekStart, biWeekStart.AddDays(14).AddTicks(-1));
            }
            case AutomationPeriod.Monthly:
            {
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                return (startOfMonth, startOfMonth.AddMonths(1).AddTicks(-1));
            }
            default:
                return (today, today.AddDays(1).AddTicks(-1));
        }
    }

    private async Task<List<Employee>> ResolveTargetEmployees(List<int>? targetEmployeeIds, CancellationToken cancellationToken)
    {
        var query = _employeeRepository.Query()
            .AsNoTracking()
            .Where(e => e.IsActive && !e.IsDeleted);

        if (targetEmployeeIds != null && targetEmployeeIds.Any())
        {
            query = query.Where(e => targetEmployeeIds.Contains(e.Id));
        }

        return await query.ToListAsync(cancellationToken);
    }

    private (bool IsValid, string? ErrorMessage) ValidateRanges(List<PointsAutomationSetting> settings)
    {
        foreach (var setting in settings)
        {
            if (!setting.Ranges.Any())
            {
                return (false, $"Automation setting '{setting.Category}:{setting.SubCategory}' has no ranges configured.");
            }

            var ranges = setting.Ranges.OrderBy(r => r.FromValue ?? decimal.MinValue).ToList();
            
            for (int i = 0; i < ranges.Count; i++)
            {
                var current = ranges[i];
                var next = i < ranges.Count - 1 ? ranges[i + 1] : null;

                if (current.FromValue.HasValue && current.ToValue.HasValue && current.FromValue > current.ToValue)
                {
                    return (false, $"Invalid range in '{setting.Category}:{setting.SubCategory}': FromValue ({current.FromValue}) cannot be greater than ToValue ({current.ToValue}).");
                }

                if (next != null && current.ToValue.HasValue && next.FromValue.HasValue && current.ToValue >= next.FromValue)
                {
                    return (false, $"Overlapping ranges in '{setting.Category}:{setting.SubCategory}': Range {i + 1} ToValue ({current.ToValue}) overlaps with Range {i + 2} FromValue ({next.FromValue}).");
                }

                if (setting.Category == AutomationCategory.Tasks && setting.SubCategory.Equals("Deadline", StringComparison.OrdinalIgnoreCase))
                {
                    var priorities = ranges.Where(r => !string.IsNullOrEmpty(r.TaskPriority))
                        .Select(r => r.TaskPriority!)
                        .Distinct()
                        .ToList();
                    
                    if (!priorities.Any())
                    {
                        return (false, $"Deadline automation setting requires TaskPriority to be configured for at least one range.");
                    }
                }
            }
        }
        return (true, null);
    }
}