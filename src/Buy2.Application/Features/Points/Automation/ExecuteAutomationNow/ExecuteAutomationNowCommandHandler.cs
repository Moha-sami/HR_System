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
    private readonly IPointsAutomationRunner _runner;
    private readonly PointsAutomationOptions _options;
    private readonly ILogger<ExecuteAutomationNowCommandHandler> _logger;

    public ExecuteAutomationNowCommandHandler(
        IRepository<PointsAutomationSetting> automationSettingRepository,
        IPointsAutomationRunner runner,
        IOptions<PointsAutomationOptions> options,
        ILogger<ExecuteAutomationNowCommandHandler> logger)
    {
        _automationSettingRepository = automationSettingRepository;
        _runner = runner;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExecuteAutomationNowResult> Handle(ExecuteAutomationNowCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Request.AutomationPeriod)
            || !Enum.TryParse<AutomationPeriod>(request.Request.AutomationPeriod.Trim(), ignoreCase: true, out var period))
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: false,
                ErrorMessage: "AutomationPeriod must be one of: Daily, Weekly, BiWeekly, Monthly.");
        }

        var hasSettings = await _automationSettingRepository.Query()
            .AsNoTracking()
            .AnyAsync(s => s.IsEnabled && s.AutomationPeriod == period, cancellationToken);

        if (!hasSettings)
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: false,
                ErrorMessage: $"No enabled automation settings found for period '{period}'.",
                IsNotFound: true);
        }

        var cairoTimeZone = CairoPeriodResolver.GetTimeZone(_options.TimeZone);
        var todayCairo = CairoPeriodResolver.TodayInCairo(DateTimeOffset.UtcNow, cairoTimeZone);

        var latestWindow = CairoPeriodResolver.TryGetNextDueWindow(period, todayCairo, lastCompletedEndCairo: null);
        if (latestWindow is null)
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: true,
                Message: $"No complete {period} window available to evaluate yet.");
        }

        var window = CairoPeriodResolver.ToUtcWindow(latestWindow.Value.Start, latestWindow.Value.End, cairoTimeZone);

        _logger.LogInformation(
            "Manual points automation triggered for {Period} [{Start} - {End}].",
            period, window.StartUtc, window.EndUtc);

        var result = await _runner.RunAsync(period, window.StartUtc, window.EndUtc, cancellationToken);

        if (result is null)
        {
            return new ExecuteAutomationNowResult(
                IsSuccess: true,
                Message: $"Nothing to evaluate: the latest {period} window [{window.StartUtc:yyyy-MM-dd} - {window.EndUtc:yyyy-MM-dd}] is already completed.");
        }

        return new ExecuteAutomationNowResult(IsSuccess: true, Data: result);
    }
}
