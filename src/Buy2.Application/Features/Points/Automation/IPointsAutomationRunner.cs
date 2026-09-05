using Buy2.Application.DTOs.Points.DTOs;
using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Points.Automation;

public interface IPointsAutomationRunner
{
    Task<AutomationJobResultDto?> RunAsync(
        AutomationCategory category,
        AutomationPeriod period,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        CancellationToken cancellationToken = default);
}
