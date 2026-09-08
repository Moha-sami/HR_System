using Buy2.Application.Common.Interfaces;
using Buy2.Application.Features.Points.Automation;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Buy2.Api.Services.PointsAutomation;

public class PointsAutomationDispatcher
{
    private readonly IPointsAutomationRunner _runner;
    private readonly IRepository<PointsAutomationSetting> _settingsRepository;
    private readonly IRepository<PointsAutomationRun> _runsRepository;
    private readonly PointsAutomationOptions _options;
    private readonly ILogger<PointsAutomationDispatcher> _logger;

    public PointsAutomationDispatcher(
        IPointsAutomationRunner runner,
        IRepository<PointsAutomationSetting> settingsRepository,
        IRepository<PointsAutomationRun> runsRepository,
        IOptions<PointsAutomationOptions> options,
        ILogger<PointsAutomationDispatcher> logger)
    {
        _runner = runner;
        _settingsRepository = settingsRepository;
        _runsRepository = runsRepository;
        _options = options.Value;
        _logger = logger;
    }

    [DisableConcurrentExecution(3600)]
    [AutomaticRetry(Attempts = 0)]
    public async Task DispatchDailyAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Points automation dispatcher skipped: disabled by configuration.");
            return;
        }

        var cairoTimeZone = CairoPeriodResolver.GetTimeZone(_options.TimeZone);
        var todayCairo = CairoPeriodResolver.TodayInCairo(DateTimeOffset.UtcNow, cairoTimeZone);

        var enabledSettings = await _settingsRepository.Query()
            .AsNoTracking()
            .Where(s => s.IsEnabled)
            .Select(s => new { s.Category, s.AutomationPeriod })
            .ToListAsync(cancellationToken);

        foreach (var group in enabledSettings.GroupBy(s => s.Category))
        {
            var category = group.Key;
            var distinctPeriods = group.Select(s => s.AutomationPeriod).Distinct().ToList();
            if (distinctPeriods.Count > 1)
            {
                _logger.LogError(
                    "Points automation skipped for category {Category}: mixed periods configured ({Periods}). Unify the period per category before running.",
                    category, string.Join(", ", distinctPeriods));
                continue;
            }

            var period = distinctPeriods[0];

            try
            {
                await RunCategoryCatchUpAsync(category, period, todayCairo, cairoTimeZone, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Points automation failed for category {Category} period {Period}. Continuing with remaining categories.",
                    category, period);
            }
        }
    }

    private async Task RunCategoryCatchUpAsync(
        AutomationCategory category,
        AutomationPeriod period,
        DateOnly todayCairo,
        TimeZoneInfo cairoTimeZone,
        CancellationToken cancellationToken = default)
    {
        var maxWindows = Math.Max(1, _options.MaxCatchUpWindowsPerPeriod);

        for (var i = 0; i < maxWindows; i++)
        {
            // Anchor on the last completed run for this category regardless of
            // the period that produced it, so switching Daily -> Weekly resumes
            // the day after the last evaluated day (no overlap, no gap).
            // Legacy runs with null Category (pre-per-category) are included
            // as fallback anchors.
            var lastCompletedEndUtc = await _runsRepository.Query()
                .AsNoTracking()
                .Where(r => (r.Category == category || r.Category == null)
                    && r.Status == AutomationRunStatus.Completed)
                .OrderByDescending(r => r.PeriodEnd)
                .Select(r => (DateTimeOffset?)r.PeriodEnd)
                .FirstOrDefaultAsync(cancellationToken);

            DateOnly? anchorEndCairo = lastCompletedEndUtc.HasValue
                ? CairoPeriodResolver.ToCairoDate(lastCompletedEndUtc.Value, cairoTimeZone)
                : null;

            var nextWindow = CairoPeriodResolver.TryGetNextDueWindow(period, todayCairo, anchorEndCairo);
            if (nextWindow is null)
            {
                return;
            }

            var window = CairoPeriodResolver.ToUtcWindow(nextWindow.Value.Start, nextWindow.Value.End, cairoTimeZone);

            _logger.LogInformation(
                "Points automation starting for category {Category} period {Period} [{Start} - {End}].",
                category, period, window.StartUtc, window.EndUtc);

            var result = await _runner.RunAsync(category, period, window.StartUtc, window.EndUtc, cancellationToken);

            if (result is null)
            {
                _logger.LogInformation(
                    "Points automation skipped for category {Category} period {Period} [{Start} - {End}]: nothing to evaluate.",
                    category, period, window.StartUtc, window.EndUtc);
                return;
            }

            _logger.LogInformation(
                "Points automation completed for category {Category} period {Period} [{Start} - {End}]: {Employees} employees, {Transactions} transactions.",
                category, period, window.StartUtc, window.EndUtc,
                result.TotalEmployeesEvaluated, result.TransactionsCreatedCount);
        }

        _logger.LogWarning(
            "Points automation for category {Category} period {Period} reached the catch-up cap ({MaxWindows} windows). Remaining windows will resume tomorrow.",
            category, period, maxWindows);
    }
}
