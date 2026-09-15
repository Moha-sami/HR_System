using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

/// <summary>
/// Data-access boundary for ApplyTemplate: all persistence reads + persistence.
/// Keeps query composition (specifications) in one place so future ORM/paging
/// changes don't ripple into business rules.
/// </summary>
public interface ITemplateApplicationLoader
{
    Task<Site?> LoadSiteAsync(int siteId, CancellationToken cancellationToken = default);

    Task<ShiftTemplate?> LoadTemplateAsync(int templateId, CancellationToken cancellationToken = default);

    Task<List<ShiftEntity>> LoadDayShiftsAsync(int siteId, DateOnly date, CancellationToken cancellationToken = default);

    Task<Dictionary<int, Employee>> LoadEmployeesAsync(List<ShiftBlock> blocks, CancellationToken cancellationToken = default);

    Task<Dictionary<int, string>> LoadRoleTitlesAsync(List<ShiftBlock> blocks, CancellationToken cancellationToken = default);

    Task<Dictionary<int, Employee>> LoadCostEmployeesAsync(
        List<ShiftEntity> finalDay,
        Dictionary<int, Employee> known,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, decimal>> LoadWeeklyHoursBeforeDayAsync(
        List<ShiftEntity> finalDay, DateOnly date, CancellationToken cancellationToken = default);

    Task PersistAsync(IEnumerable<PlannedBlock> planned, CancellationToken cancellationToken = default);
}
