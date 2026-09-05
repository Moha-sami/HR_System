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
    public async Task DispatchDailyAsync()
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Points automation dispatcher skipped: disabled by configuration.");
            return;
        }

        var cairoTimeZone = CairoPeriodResolver.GetTimeZone(_options.TimeZone);
        var todayCairo = CairoPeriodResolver.TodayInCairo(DateTimeOffset.UtcNow, cairoTimeZone);

        var duePeriods = await _settingsRepository.Query()
            .AsNoTracking()
            .Where(s => s.IsEnabled)
            .Select(s => s.AutomationPeriod)
            .Distinct()
            .ToListAsync();

        foreach (var period in duePeriods)
        {
            try
            {
                await RunPeriodCatchUpAsync(period, todayCairo, cairoTimeZone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Points automation failed for {Period}. Continuing with remaining periods.",
                    period);
            }
        }
    }

    private async Task RunPeriodCatchUpAsync(
        AutomationPeriod period,
        DateOnly todayCairo,
        TimeZoneInfo cairoTimeZone)
    {
        var maxWindows = Math.Max(1, _options.MaxCatchUpWindowsPerPeriod);

        for (var i = 0; i < maxWindows; i++)
        {
            var lastCompletedEndUtc = await _runsRepository.Query()
                .AsNoTracking()
                .Where(r => r.AutomationPeriod == period && r.Status == AutomationRunStatus.Completed)
                .OrderByDescending(r => r.PeriodEnd)
                .Select(r => (DateTimeOffset?)r.PeriodEnd)
                .FirstOrDefaultAsync();

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
                "Points automation starting for {Period} [{Start} - {End}].",
                period, window.StartUtc, window.EndUtc);

            var result = await _runner.RunAsync(period, window.StartUtc, window.EndUtc);

            if (result is null)
            {
                _logger.LogInformation(
                    "Points automation skipped for {Period} [{Start} - {End}]: nothing to evaluate.",
                    period, window.StartUtc, window.EndUtc);
                return;
            }

            _logger.LogInformation(
                "Points automation completed for {Period} [{Start} - {End}]: {Employees} employees, {Transactions} transactions.",
                period, window.StartUtc, window.EndUtc,
                result.TotalEmployeesEvaluated, result.TransactionsCreatedCount);
        }

        _logger.LogWarning(
            "Points automation for {Period} reached the catch-up cap ({MaxWindows} windows). Remaining windows will resume tomorrow.",
            period, maxWindows);
    }
}
