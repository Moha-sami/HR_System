using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Schedules;
using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

public sealed class OverlapResolver : IOverlapResolver
{
    private readonly IRepository<ShiftEntity> _shiftRepository;

    public OverlapResolver(IRepository<ShiftEntity> shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public KeepMode? ParseKeepMode(string? keep)
    {
        if (string.IsNullOrWhiteSpace(keep))
        {
            return KeepMode.None;
        }

        if (keep.Trim().Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            return KeepMode.KeepNew;
        }

        if (keep.Trim().Equals("existing", StringComparison.OrdinalIgnoreCase))
        {
            return KeepMode.KeepExisting;
        }

        return null;
    }

    public void DetectOverlaps(List<PlannedBlock> planned, List<ShiftEntity> existingShifts)
    {
        var candidates = FindOverlapCandidates(planned, existingShifts);
        var seen = new List<ShiftEntity>(candidates);
        var existingIds = candidates.Select(s => s.Id).ToHashSet();

        foreach (var block in planned)
        {
            FlagBlockOverlaps(block, seen, existingIds);
            seen.Add(block.Entity);
        }
    }

    public async Task<HashSet<int>> ApplyKeepResolutionAsync(
        List<PlannedBlock> planned,
        KeepMode keep,
        CancellationToken cancellationToken = default)
    {
        var deletedIds = new HashSet<int>();

        if (keep == KeepMode.KeepNew)
        {
            await DeleteOverlappingExistingAsync(planned, deletedIds, cancellationToken);
        }

        if (keep == KeepMode.KeepExisting)
        {
            PruneCollidingNew(planned);
        }

        return deletedIds;
    }

    private static List<ShiftEntity> FindOverlapCandidates(
        List<PlannedBlock> planned, List<ShiftEntity> existingShifts)
    {
        if (planned.Count == 0)
        {
            return new List<ShiftEntity>();
        }

        if (existingShifts.Count == 0)
        {
            return new List<ShiftEntity>();
        }

        var minStart = planned.Min(p => p.Entity.StartTime);
        var maxEnd = planned.Max(p => p.Entity.EndTime);

        return existingShifts
            .Where(s => s.StartTime < maxEnd && s.EndTime > minStart)
            .ToList();
    }

    private static void FlagBlockOverlaps(
        PlannedBlock block, List<ShiftEntity> seen, HashSet<int> existingIds)
    {
        foreach (var other in seen)
        {
            if (!Overlaps(block.Entity, other))
            {
                continue;
            }

            if (existingIds.Contains(other.Id))
            {
                block.OverlappingExistingIds.Add(other.Id);
            }

            FlagCollisionOrConflict(block, other);
        }
    }

    private static void FlagCollisionOrConflict(PlannedBlock block, ShiftEntity other)
    {
        if (block.Entity.EmployeeId.HasValue && other.EmployeeId == block.Entity.EmployeeId)
        {
            block.Collision = true;
            block.CollisionType = ApplyTemplateCollisionTypes.EmployeeOverlap;
        }
        else
        {
            block.Conflict = true;
            block.ConflictType = ApplyTemplateConflictTypes.RoleSlotConflict;
        }
    }

    private static bool Overlaps(ShiftEntity first, ShiftEntity second)
    {
        return first.StartTime < second.EndTime && second.StartTime < first.EndTime;
    }

    private async Task DeleteOverlappingExistingAsync(
        List<PlannedBlock> planned,
        HashSet<int> deletedIds,
        CancellationToken cancellationToken)
    {
        var targets = planned
            .SelectMany(p => p.OverlappingExistingIds)
            .Distinct()
            .ToList();

        if (targets.Count == 0)
        {
            return;
        }

        var specification = new Specification<ShiftEntity>()
            .Where(s => targets.Contains(s.Id))
            .AsTracked();

        var shifts = await _shiftRepository.ListAsync(specification, cancellationToken);

        foreach (var shift in shifts)
        {
            _shiftRepository.Delete(shift);
            deletedIds.Add(shift.Id);
        }
    }

    private static void PruneCollidingNew(List<PlannedBlock> planned)
    {
        foreach (var block in planned)
        {
            if (block.Collision || block.Conflict)
            {
                block.Pruned = true;
            }
        }
    }
}
