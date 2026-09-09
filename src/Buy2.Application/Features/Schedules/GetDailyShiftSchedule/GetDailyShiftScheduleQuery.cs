using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.GetDailyShiftSchedule;

public record GetDailyShiftScheduleQuery(int SiteId, DateOnly Date) : IRequest<DailyShiftScheduleResponseDto>;
