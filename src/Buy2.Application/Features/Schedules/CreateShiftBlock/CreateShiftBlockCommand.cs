using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.CreateShiftBlock;

public record CreateShiftBlockCommand(
    int SiteId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int JobRoleId,
    ShiftMarketDispatchPolicy DispatchPolicy = ShiftMarketDispatchPolicy.None
) : IRequest<ShiftBlockResponseDto>;
