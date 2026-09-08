using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Application.Features.Points.Automation.Evaluators;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Buy2.Application.Features.Points.Automation;

public class PointsAutomationRunner : IPointsAutomationRunner
{
    private readonly IRepository<PointsAutomationSetting> _automationSettingRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AttendanceRecord> _attendanceRecordRepository;
    private readonly IRepository<EmployeeTask> _employeeTaskRepository;
    private readonly IRepository<PerformanceSubmission> _performanceSubmissionRepository;
    private readonly IRepository<PointsTransaction> _pointsTransactionRepository;
    private readonly IRepository<PointsAutomationRun> _automationRunRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IAutomationEvaluator> _evaluators;
    private readonly ILogger<PointsAutomationRunner> _logger;

    public PointsAutomationRunner(
        IRepository<PointsAutomationSetting> automationSettingRepository,
        IRepository<Employee> employeeRepository,
        IRepository<AttendanceRecord> attendanceRecordRepository,
        IRepository<EmployeeTask> employeeTaskRepository,
        IRepository<PerformanceSubmission> performanceSubmissionRepository,
        IRepository<PointsTransaction> pointsTransactionRepository,
        IRepository<PointsAutomationRun> automationRunRepository,
        IUnitOfWork unitOfWork,
        IEnumerable<IAutomationEvaluator> evaluators,
        ILogger<PointsAutomationRunner> logger)
    {
        _automationSettingRepository = automationSettingRepository;
        _employeeRepository = employeeRepository;
        _attendanceRecordRepository = attendanceRecordRepository;
        _employeeTaskRepository = employeeTaskRepository;
        _performanceSubmissionRepository = performanceSubmissionRepository;
        _pointsTransactionRepository = pointsTransactionRepository;
        _automationRunRepository = automationRunRepository;
        _unitOfWork = unitOfWork;
        _evaluators = evaluators;
        _logger = logger;
    }

    public async Task<AutomationJobResultDto?> RunAsync(
        AutomationCategory category,
        AutomationPeriod period,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var executedAt = DateTimeOffset.UtcNow;

        var categorySettings = await _automationSettingRepository.Query()
            .AsNoTracking()
            .Include(s => s.Ranges)
            .Where(s => s.IsEnabled && s.Category == category)
            .ToListAsync(cancellationToken);

        if (!categorySettings.Any())
        {
            _logger.LogInformation(
                "Points automation skipped for category {Category}: no enabled settings.",
                category);
            return null;
        }

        var distinctPeriods = categorySettings.Select(s => s.AutomationPeriod).Distinct().ToList();
        if (distinctPeriods.Count > 1)
        {
            var message = $"Automation settings for category '{category}' have mixed periods " +
                $"({string.Join(", ", distinctPeriods)}). Unify the period per category before running.";
            await RecordRunAsync(
                category, period, periodStartUtc, periodEndUtc, executedAt,
                AutomationRunStatus.Failed, 0, 0, message,
                cancellationToken);
            throw new InvalidOperationException(message);
        }

        var settings = categorySettings.Where(s => s.AutomationPeriod == period).ToList();
        if (!settings.Any())
        {
            _logger.LogInformation(
                "Points automation skipped for category {Category} period {Period}: no enabled settings for this period.",
                category, period);
            return null;
        }

        var alreadyCompleted = await _automationRunRepository.Query()
            .AsNoTracking()
            .AnyAsync(
                r => r.Category == category
                    && r.PeriodStart == periodStartUtc
                    && r.PeriodEnd == periodEndUtc
                    && r.Status == AutomationRunStatus.Completed,
                cancellationToken);

        if (alreadyCompleted)
        {
            _logger.LogInformation(
                "Points automation skipped for category {Category} period {Period} [{Start} - {End}]: already completed.",
                category, period, periodStartUtc, periodEndUtc);
            return null;
        }

        var validationResult = ValidateRanges(settings);
        if (!validationResult.IsValid)
        {
            await RecordRunAsync(
                category, period, periodStartUtc, periodEndUtc, executedAt,
                AutomationRunStatus.Failed, 0, 0, validationResult.ErrorMessage,
                cancellationToken);
            throw new InvalidOperationException(validationResult.ErrorMessage);
        }

        var targetEmployees = await _employeeRepository.Query()
            .AsNoTracking()
            .Where(e => e.IsActive && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!targetEmployees.Any())
        {
            await RecordRunAsync(
                category, period, periodStartUtc, periodEndUtc, executedAt,
                AutomationRunStatus.Failed, 0, 0, "No active employees found for evaluation.",
                cancellationToken);
            throw new InvalidOperationException("No active employees found for evaluation.");
        }

        var employeeIds = targetEmployees.Select(e => e.Id).ToList();

        var evaluator = _evaluators.FirstOrDefault(e => e.Category == category);
        if (evaluator is null)
        {
            var evaluatorMessage = $"No automation evaluator registered for category '{category}'.";
            await RecordRunAsync(
                category, period, periodStartUtc, periodEndUtc, executedAt,
                AutomationRunStatus.Failed, 0, 0, evaluatorMessage,
                cancellationToken);
            throw new InvalidOperationException(evaluatorMessage);
        }

        var evaluatorSettings = settings.Where(s => s.Category == category).ToList();

        var existingTransactions = await _pointsTransactionRepository.Query()
            .AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                && t.TriggeredBy == "KPI Achievement"
                && t.AutomationCategory == category
                && t.EvaluationPeriodStart == periodStartUtc
                && t.EvaluationPeriodEnd == periodEndUtc)
            .Select(t => t.EmployeeId)
            .ToListAsync(cancellationToken);

        var existingTransactionKeys = existingTransactions
            .Select(employeeId => (employeeId, category))
            .ToHashSet();

        var attendanceRecords = await _attendanceRecordRepository.Query()
            .AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId)
                && r.Date >= periodStartUtc.Date
                && r.Date <= periodEndUtc.Date)
            .ToListAsync(cancellationToken);

        var employeeTasks = await _employeeTaskRepository.Query()
            .AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeId)
                && t.DueDate.HasValue
                && t.DueDate.Value.Date >= periodStartUtc.Date
                && t.DueDate.Value.Date <= periodEndUtc.Date)
            .ToListAsync(cancellationToken);

        var performanceSubmissions = await _performanceSubmissionRepository.Query()
            .AsNoTracking()
            .Include(s => s.PerformanceMetric)
            .Where(s => employeeIds.Contains(s.EmployeeId)
                && s.SubmissionDate >= periodStartUtc.Date
                && s.SubmissionDate <= periodEndUtc.Date)
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
            var transactionKey = (employee.Id, category);
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
                    PeriodStart = periodStartUtc,
                    PeriodEnd = periodEndUtc,
                    AttendanceRecords = attendanceByEmployee.GetValueOrDefault(employee.Id, new List<AttendanceRecord>()),
                    Tasks = tasksByEmployee.GetValueOrDefault(employee.Id, new List<EmployeeTask>()),
                    Submissions = submissionsByEmployee.GetValueOrDefault(employee.Id, new List<PerformanceSubmission>())
                };

                var (points, ruleEvaluations) = await evaluator.EvaluateAsync(context, evaluatorSettings, cancellationToken);

                if (points != 0 && ruleEvaluations.Any())
                {
                    var transactionType = points >= 0 ? TransactionType.Add : TransactionType.Deduct;

                    var transaction = new PointsTransaction
                    {
                        EmployeeId = employee.Id,
                        Amount = points,
                        TransactionType = transactionType,
                        TriggeredBy = "KPI Achievement",
                        Comments = $"{category} Automation",
                        EvaluationPeriodStart = periodStartUtc,
                        EvaluationPeriodEnd = periodEndUtc,
                        AutomationCategory = category,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    await _pointsTransactionRepository.AddAsync(transaction, cancellationToken);

                    if (points > 0) totalPointsAwarded += points;
                    else totalPointsDeducted += Math.Abs(points);

                    successfulEvaluations.Add(new EmployeeAutomationSuccessDto(
                        EmployeeId: employee.Id,
                        Category: category.ToString(),
                        PointsAmount: points,
                        TransactionId: transaction.Id,
                        EvaluationPeriodStart: periodStartUtc,
                        EvaluationPeriodEnd: periodEndUtc,
                        RuleEvaluations: ruleEvaluations
                    ));

                    transactionsCreated++;
                    existingTransactionKeys.Add(transactionKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Points automation evaluation failed for employee {EmployeeId}, category {Category}.",
                    employee.Id, category);
                employeeFailures.Add(new EmployeeAutomationFailureDto(
                    EmployeeId: employee.Id,
                    Category: category.ToString(),
                    ErrorMessage: ex.Message
                ));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecordRunAsync(
            category, period, periodStartUtc, periodEndUtc, executedAt,
            AutomationRunStatus.Completed, targetEmployees.Count, transactionsCreated, null,
            cancellationToken);

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
            SummaryNotes: $"{category} {period} automation executed for {targetEmployees.Count} employees. {skippedEmployeeIds.Count} skipped, {employeeFailures.Count} failures."
        );

        return result;
    }

    private async Task RecordRunAsync(
        AutomationCategory category,
        AutomationPeriod period,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        DateTimeOffset executedAt,
        AutomationRunStatus status,
        int employeesEvaluated,
        int transactionsCreated,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var run = new PointsAutomationRun
        {
            Category = category,
            AutomationPeriod = period,
            PeriodStart = periodStartUtc,
            PeriodEnd = periodEndUtc,
            Status = status,
            EmployeesEvaluated = employeesEvaluated,
            TransactionsCreated = transactionsCreated,
            ExecutedAt = executedAt,
            ErrorMessage = errorMessage
        };

        await _automationRunRepository.AddAsync(run, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
