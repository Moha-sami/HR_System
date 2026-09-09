using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.AutoFillDailyShifts;

public record AutoFillDailyShiftsCommand(
    int SiteId,
    DateOnly Date
) : IRequest<AutoFillDailyShiftsResponseDto>;
