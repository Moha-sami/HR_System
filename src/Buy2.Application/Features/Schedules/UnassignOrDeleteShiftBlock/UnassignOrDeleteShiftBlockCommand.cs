using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.UnassignOrDeleteShiftBlock;

public record UnassignOrDeleteShiftBlockCommand(
    int ShiftBlockId,
    ShiftBlockRemovalAction Action = ShiftBlockRemovalAction.UnassignOnly,
    bool ConfirmPublishedDeletion = false
) : IRequest<UnassignOrDeleteShiftBlockResponseDto>;
