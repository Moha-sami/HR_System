using Buy2.Domain.Entities;

namespace Buy2.Application.Features.Schedules.ApplyTemplate.Services;

/// <summary>
/// Overlap detection + keep-policy resolution (new / existing).
/// Owns only ShiftEntity persistence for the KeepNew delete path.
/// </summary>
public interface IOverlapResolver
{
    KeepMode? ParseKeepMode(string? keep);

    void DetectOverlaps(List<PlannedBlock> planned, List<ShiftEntity> existingShifts);

    Task<HashSet<int>> ApplyKeepResolutionAsync(
        List<PlannedBlock> planned,
        KeepMode keep,
        CancellationToken cancellationToken = default);
}
