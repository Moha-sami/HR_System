using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

/// <summary>
/// Resolves employee availability for a site/day:
/// authorization + operational hours + approved leave / remote / attendance records.
/// </summary>
public interface IAvailabilityResolver
{
    Task<AvailabilityContext> ResolveAsync(
        int siteId,
        Site site,
        DateOnly date,
        IEnumerable<int> candidateEmployeeIds,
        CancellationToken cancellationToken = default);
}
