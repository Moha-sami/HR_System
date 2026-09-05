using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using Buy2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Buy2.Application.Features.Points.Automation.ExecuteAutomationNow;

public class ExecuteAutomationNowCommandHandler : IRequestHandler<ExecuteAutomationNowCommand, ExecuteAutomationNowResult>
{
    private readonly IRepository<PointsAutomationSetting> _automationSettingRepository;
    private readonly IRepository<PointsAutomationRun> _automationRunRepository;
    private readonly IPointsAutomationRunner _runner;
    private readonly PointsAutomationOptions _options;
    private readonly ILogger<ExecuteAutomationNowCommandHandler> _logger;

    public ExecuteAutomationNowCommandHandler(
        IRepository<PointsAutomationSetting> automationSettingRepository,
        IRepository<PointsAutomationRun> automationRunRepository,
        IPointsAutomationRunner runner,
        IOptions<PointsAutomationOptions> options,
        ILogger<ExecuteAutomationNowCommandHandler> logger)
    {
        _automationSettingRepository = automationSettingRepository;
        _automationRunRepository = automationRunRepository;
        _runner = runner;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExecuteAutomationNowResult> Handle(ExecuteAutomationNowCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Request.Category)
            || !Enum.TryParse<AutomationCategory>(request.Request.Category.Trim(), ignoreCase: true, out var category))
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: false,
                ErrorMessage: "Category must be one of: Performance, Tasks, TimeAndAttendance.");
        }

        if (string.IsNullOrWhiteSpace(request.Request.AutomationPeriod)
            || !Enum.TryParse<AutomationPeriod>(request.Request.AutomationPeriod.Trim(), ignoreCase: true, out var period))
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: false,
                ErrorMessage: "AutomationPeriod must be one of: Daily, Weekly, BiWeekly, Monthly.");
        }

        var hasSettings = await _automationSettingRepository.Query()
            .AsNoTracking()
            .AnyAsync(s => s.IsEnabled && s.Category == category && s.AutomationPeriod == period, cancellationToken);

        if (!hasSettings)
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: false,
                ErrorMessage: $"No enabled automation settings found for category '{category}' with period '{period}'.",
                IsNotFound: true);
        }

        var cairoTimeZone = CairoPeriodResolver.GetTimeZone(_options.TimeZone);
        var todayCairo = CairoPeriodResolver.TodayInCairo(DateTimeOffset.UtcNow, cairoTimeZone);

        // Anchor on the last completed run for this category regardless of the
        // period that produced it (Daily on Monday -> Weekly starts Tuesday).
        var lastCompletedEndUtc = await _automationRunRepository.Query()
            .AsNoTracking()
            .Where(r => (r.Category == category || r.Category == null)
                && r.Status == AutomationRunStatus.Completed)
            .OrderByDescending(r => r.PeriodEnd)
            .Select(r => (DateTimeOffset?)r.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);

        DateOnly? anchorEndCairo = lastCompletedEndUtc.HasValue
            ? CairoPeriodResolver.ToCairoDate(lastCompletedEndUtc.Value, cairoTimeZone)
            : null;

        var latestWindow = CairoPeriodResolver.TryGetNextDueWindow(period, todayCairo, anchorEndCairo);
        if (latestWindow is null)
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: true,
                Message: $"No complete {category} {period} window available to evaluate yet.");
        }

        var window = CairoPeriodResolver.ToUtcWindow(latestWindow.Value.Start, latestWindow.Value.End, cairoTimeZone);

        _logger.LogInformation(
            "Manual points automation triggered for category {Category} period {Period} [{Start} - {End}].",
            category, period, window.StartUtc, window.EndUtc);

        var result = await _runner.RunAsync(category, period, window.StartUtc, window.EndUtc, cancellationToken);

        if (result is null)
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: true,
                Message: $"Nothing to evaluate: the latest {category} {period} window [{window.StartUtc:yyyy-MM-dd} - {window.EndUtc:yyyy-MM-dd}] is already completed.");
        }

        return new ExecuteAutomationNowResult(IsSuccess: true, Data: result);
    }
}
