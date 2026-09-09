using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.CommitCopyShifts;

public record CommitCopyShiftsCommand(
    int SiteId,
    DateOnly SourceDate,
    List<DateOnly>? TargetDates = null,
    Dictionary<DateOnly, CopyConflictResolution>? DateResolutions = null,
    bool BulkReplaceAll = false,
    bool CopyAssignments = true
) : IRequest<CommitCopyShiftsResponseDto>;
