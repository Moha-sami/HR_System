using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

/// <summary>
/// Pure eligibility rules: which template blocks keep their assignment
/// and which are stripped to open roles (with code + reason).
/// No repository access — fully unit-testable.
/// </summary>
public interface IEligibilityEvaluator
{
    List<PlannedBlock> BuildPlannedBlocks(
        int siteId,
        DateOnly date,
        int templateId,
        List<ShiftBlock> blocks,
        Dictionary<int, Employee> employees,
        Dictionary<int, string> roleTitles,
        AvailabilityContext availability);
}
