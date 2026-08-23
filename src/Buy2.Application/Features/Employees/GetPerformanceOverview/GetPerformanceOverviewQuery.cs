using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Employees.GetPerformanceOverview;

public record GetPerformanceOverviewQuery(
    int EmployeeId,
    string? Period = null,
    int? Days = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
) : IRequest<PerformanceOverviewDto?>;

public class GetPerformanceOverviewQueryHandler : IRequestHandler<GetPerformanceOverviewQuery, PerformanceOverviewDto?>
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<PerformanceSubmission> _submissionRepository;
    private readonly IRepository<EmployeeTask> _taskRepository;
    private readonly IRepository<EmployeeAchievement> _achievementRepository;

    public GetPerformanceOverviewQueryHandler(
        IRepository<Employee> employeeRepository,
        IRepository<PerformanceSubmission> submissionRepository,
        IRepository<EmployeeTask> taskRepository,
        IRepository<EmployeeAchievement> achievementRepository)
    {
        _employeeRepository = employeeRepository;
        _submissionRepository = submissionRepository;
        _taskRepository = taskRepository;
        _achievementRepository = achievementRepository;
    }

    public async Task<PerformanceOverviewDto?> Handle(GetPerformanceOverviewQuery request, CancellationToken cancellationToken)
    {
        // 1. Validate employee existence and active status (not soft-deleted)
        var employee = await _employeeRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        // 2. Resolve Date Range
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

        // 3. Query Performance Submissions with Metric details
        var submissions = await _submissionRepository.Query()
            .AsNoTracking()
            .Include(s => s.PerformanceMetric)
            .Where(s => s.EmployeeId == request.EmployeeId &&
                        s.SubmissionDate >= fromUtcDateTime &&
                        s.SubmissionDate <= toUtcDateTime)
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync(cancellationToken);

        // 4. Calculate Overall Weighted Score and Rating Label
        decimal overallWeightedScore = 0m;
        decimal totalWeight = 0m;
        decimal weightedSum = 0m;

        foreach (var sub in submissions)
        {
            var weight = sub.PerformanceMetric?.Weight ?? 1m;
            weightedSum += sub.Score * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0)
        {
            overallWeightedScore = Math.Round(weightedSum / totalWeight, 2);
        }
        else if (submissions.Count > 0)
        {
            overallWeightedScore = Math.Round(submissions.Average(s => s.Score), 2);
        }

        string ratingLabel;
        if (overallWeightedScore < 40m)
        {
            ratingLabel = "Needs Improvement";
        }
        else if (overallWeightedScore < 70m)
        {
            ratingLabel = "Satisfactory";
        }
        else if (overallWeightedScore < 90m)
        {
            ratingLabel = "Good Performance";
        }
        else
        {
            ratingLabel = "Excellent";
        }

        // 5. Query Tasks and Calculate Task Statistics within Range
        var tasks = await _taskRepository.Query()
            .AsNoTracking()
            .Where(t => t.EmployeeId == request.EmployeeId &&
                        t.CreatedAt <= toUtcDateTime &&
                        (t.DueDate == null || t.DueDate >= fromUtcDateTime))
            .ToListAsync(cancellationToken);

        var totalTasks = tasks.Count;
        var todoCount = tasks.Count(t => t.Status == EmployeeTaskStatus.Todo);
        var inProgressCount = tasks.Count(t => t.Status == EmployeeTaskStatus.InProgress || t.Status == EmployeeTaskStatus.InReview);
        var completedCount = tasks.Count(t => t.Status == EmployeeTaskStatus.Completed);
        var overdueCount = tasks.Count(t => t.Status == EmployeeTaskStatus.Overdue ||
            (t.Status != EmployeeTaskStatus.Completed && t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow));

        var tasksWithDueDate = tasks.Where(t => t.DueDate.HasValue).ToList();
        var completedWithDueDate = tasksWithDueDate.Count(t => t.Status == EmployeeTaskStatus.Completed);
        var overdueWithDueDate = tasksWithDueDate.Count(t => t.Status == EmployeeTaskStatus.Overdue ||
            (t.Status != EmployeeTaskStatus.Completed && t.DueDate!.Value < DateTime.UtcNow));

        decimal deadlineCompliancePercentage = 0m;
        if (completedWithDueDate + overdueWithDueDate > 0)
        {
            deadlineCompliancePercentage = Math.Round((decimal)completedWithDueDate / (completedWithDueDate + overdueWithDueDate) * 100m, 2);
        }
        else if (totalTasks > 0 && overdueCount == 0)
        {
            deadlineCompliancePercentage = 100m;
        }

        var tasksSummary = new TasksSummaryDto(
            TotalTasks: totalTasks,
            TodoCount: todoCount,
            InProgressCount: inProgressCount,
            CompletedCount: completedCount,
            OverdueCount: overdueCount,
            DeadlineCompliancePercentage: deadlineCompliancePercentage
        );

        // 6. Query Employee Achievements including Badge navigation
        var achievements = await _achievementRepository.Query()
            .AsNoTracking()
            .Include(a => a.Badge)
            .Where(a => a.EmployeeId == request.EmployeeId)
            .OrderByDescending(a => a.AwardedAt)
            .ToListAsync(cancellationToken);

        var achievementBadges = achievements
            .Select(MapAchievementToDto)
            .ToList();

        // 7. Calculate Chart Trend Points (Submissions grouped by date)
        var chartTrendPoints = submissions
            .GroupBy(s => s.SubmissionDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ChartPointDto(
                Date: new DateTimeOffset(DateTime.SpecifyKind(g.Key, DateTimeKind.Utc)),
                Score: Math.Round(g.Average(x => x.Score), 2)
            ))
            .ToList();

        // 8. Individual Submissions Details (capped at 200)
        var submissionsDetail = submissions
            .Take(200)
            .Select(s => new SubmissionDetailDto(
                Id: s.Id,
                MetricName: s.PerformanceMetric?.Name ?? $"Metric #{s.MetricId}",
                Score: s.Score,
                Weight: s.PerformanceMetric?.Weight ?? 1m,
                SubmittedAt: new DateTimeOffset(DateTime.SpecifyKind(s.SubmissionDate, DateTimeKind.Utc)),
                Notes: s.Feedback ?? string.Empty
            )).ToList();

        // 9. Assemble and return full PerformanceOverviewDto
        return new PerformanceOverviewDto(
            EmployeeId: employee.Id,
            DateRangeResolved: new DateRangeResolvedDto(fromDate, toDate, periodResolved),
            OverallWeightedScore: overallWeightedScore,
            RatingLabel: ratingLabel,
            TasksSummary: tasksSummary,
            Achievements: achievementBadges,
            ChartTrendPoints: chartTrendPoints,
            SubmissionsDetail: submissionsDetail
        );
    }

    private static AchievementBadgeDto MapAchievementToDto(EmployeeAchievement achievement)
    {
        var badge = achievement.Badge;
        var id = badge?.Id ?? achievement.Id;

        var title = !string.IsNullOrWhiteSpace(badge?.Name)
            ? badge.Name
            : (!string.IsNullOrWhiteSpace(achievement.BadgeType) ? achievement.BadgeType : "Achievement");

        var description = !string.IsNullOrWhiteSpace(badge?.Description)
            ? badge.Description
            : (!string.IsNullOrWhiteSpace(achievement.BadgeType) ? $"Awarded badge: {achievement.BadgeType}" : "Achievement awarded");

        var iconUrl = badge?.IconUrl;
        var pointsAwarded = achievement.PointsAwarded != 0
            ? achievement.PointsAwarded
            : (badge?.PointsAwarded ?? 0);

        var earnedAt = new DateTimeOffset(DateTime.SpecifyKind(achievement.AwardedAt, DateTimeKind.Utc));

        return new AchievementBadgeDto(
            Id: id,
            Title: title,
            Description: description,
            IconUrl: iconUrl,
            PointsAwarded: pointsAwarded,
            EarnedAt: earnedAt
        );
    }
}
