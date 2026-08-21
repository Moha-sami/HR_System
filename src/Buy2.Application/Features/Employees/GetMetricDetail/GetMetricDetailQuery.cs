using System.Globalization;
using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetMetricDetail;

public record GetMetricDetailQuery(
    int EmployeeId,
    int MetricId,
    string? Period = null,
    int? Days = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
) : IRequest<MetricDetailDto?>;

public class GetMetricDetailQueryHandler : IRequestHandler<GetMetricDetailQuery, MetricDetailDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PerformanceMetric> _metricRepository;
    private readonly IRepository<PerformanceSubmission> _submissionRepository;

    public GetMetricDetailQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PerformanceMetric> metricRepository,
        IRepository<PerformanceSubmission> submissionRepository)
    {
        _employeeRepository = employeeRepository;
        _metricRepository = metricRepository;
        _submissionRepository = submissionRepository;
    }

    public async Task<MetricDetailDto?> Handle(GetMetricDetailQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate employee existence and active status (not soft-deleted)
        var employee = await _employeeRepository.Query()
            .Include(e => e.DirectManager)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Validate performance metric existence
        var metric = await _metricRepository.Query()
            .FirstOrDefaultAsync(m => m.Id == request.MetricId, cancellationToken);

        if (metric == null)
        {
            return null;
        }

        // 3. Fetch all submissions for this Employee and Metric (all-time)
        var allSubmissions = await _submissionRepository.Query()
            .Where(s => s.EmployeeId == request.EmployeeId && s.MetricId == request.MetricId)
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync(cancellationToken);

        // Helper functions for scoring
        static decimal CalculateAverageScore(IReadOnlyCollection<PerformanceSubmission> submissions)
        {
            if (submissions.Count == 0)
            {
                return 0m;
            }

            return Math.Round(submissions.Average(s => s.Score), 2);
        }

        static string GetRatingLabel(decimal score) => score switch
        {
            < 40m => "Needs Improvement",
            < 70m => "Satisfactory",
            < 90m => "Good Performance",
            _     => "Excellent"
        };

        // 4. Calculate AllTimeAverageScore
        var allTimeAverageScore = CalculateAverageScore(allSubmissions);

        // 5. Resolve Date Range
        var nowUtc = DateTimeOffset.UtcNow;
        DateTimeOffset fromDate;
        DateTimeOffset toDate = nowUtc;
        string? periodResolved = request.Period;

        if (!string.IsNullOrWhiteSpace(request.Period))
        {
            var periodKey = request.Period.Trim().ToLowerInvariant();
            switch (periodKey)
            {
                case "today":
                    fromDate = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero);
                    toDate = fromDate.AddDays(1).AddTicks(-1);
                    periodResolved = "today";
                    break;
                case "thisweek":
                case "week":
                    int diff = (7 + (int)nowUtc.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                    var mondayDate = nowUtc.Date.AddDays(-diff);
                    fromDate = new DateTimeOffset(mondayDate.Year, mondayDate.Month, mondayDate.Day, 0, 0, 0, TimeSpan.Zero);
                    toDate = nowUtc;
                    periodResolved = "thisWeek";
                    break;
                case "thismonth":
                case "month":
                    fromDate = new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);
                    toDate = nowUtc;
                    periodResolved = "thisMonth";
                    break;
                case "thisyear":
                case "year":
                    fromDate = new DateTimeOffset(nowUtc.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    toDate = nowUtc;
                    periodResolved = "thisYear";
                    break;
                default:
                    fromDate = new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);
                    toDate = nowUtc;
                    periodResolved = "thisMonth";
                    break;
            }
        }
        else if (request.Days.HasValue && request.Days.Value > 0)
        {
            var clampedDays = Math.Min(request.Days.Value, 3650);
            fromDate = nowUtc.AddDays(-clampedDays);
            toDate = nowUtc;
            periodResolved = $"{clampedDays}days";
        }
        else if (request.From.HasValue || request.To.HasValue)
        {
            fromDate = request.From ?? new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);
            toDate = request.To ?? nowUtc;
            if (Math.Abs((toDate - fromDate).TotalDays) > 3650)
            {
                fromDate = toDate.AddDays(-3650);
            }
            periodResolved = "custom";
        }
        else
        {
            fromDate = new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);
            toDate = nowUtc;
            periodResolved = "thisMonth";
        }

        if (fromDate > toDate)
        {
            (fromDate, toDate) = (toDate, fromDate);
        }

        var fromUtcDateTime = fromDate.UtcDateTime;
        var toUtcDateTime = toDate.UtcDateTime;

        // 6. Filter submissions within date range for PeriodAverageScore and Rating Label
        var periodSubmissions = allSubmissions
            .Where(s => s.SubmissionDate >= fromUtcDateTime && s.SubmissionDate <= toUtcDateTime)
            .ToList();

        var periodAverageScore = CalculateAverageScore(periodSubmissions);
        var periodRatingLabel = GetRatingLabel(periodAverageScore);

        // 7. Group all-time submissions by (Year, Month) ordered chronologically
        var monthlyTrends = allSubmissions
            .GroupBy(s => new { s.SubmissionDate.Year, s.SubmissionDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyTrendPointDto(
                Year: g.Key.Year,
                Month: g.Key.Month,
                YearMonthLabel: new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture),
                AverageScore: Math.Round(g.Average(s => s.Score), 2),
                SubmissionCount: g.Count()
            ))
            .ToList();

        // 8. Build Submissions list from the period-filtered submissions
        string? evaluatorName = employee.DirectManager != null
            ? $"{employee.DirectManager.FirstName} {employee.DirectManager.LastName}".Trim()
            : null;

        var periodSubmissionItems = periodSubmissions
            .OrderByDescending(s => s.SubmissionDate)
            .Select(s => new MetricSubmissionItemDto(
                Id: s.Id,
                Score: s.Score,
                SubmittedAt: new DateTimeOffset(DateTime.SpecifyKind(s.SubmissionDate, DateTimeKind.Utc)),
                Notes: s.Feedback ?? string.Empty,
                EvaluatorName: evaluatorName
            ))
            .ToList();

        // 9. Assemble and return full MetricDetailDto
        return new MetricDetailDto(
            EmployeeId: employee.Id,
            MetricId: metric.Id,
            MetricName: metric.Name,
            MetricDescription: metric.Description ?? string.Empty,
            Weight: metric.Weight,
            TargetScore: metric.Target,
            Unit: "%",
            AllTimeAverageScore: allTimeAverageScore,
            PeriodAverageScore: periodAverageScore,
            PeriodRatingLabel: periodRatingLabel,
            DateRangeResolved: new DateRangeResolvedDto(fromDate, toDate, periodResolved),
            MonthlyTrends: monthlyTrends,
            Submissions: periodSubmissionItems
        );
    }
}
