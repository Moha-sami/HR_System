using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.AssignEmployeeToShift;

public record AssignEmployeeToShiftCommand(
    int ShiftBlockId,
    int EmployeeId,
    bool ConfirmOverride = false,
    string? OverrideReason = null
) : IRequest<AssignEmployeeToShiftResponseDto>;
