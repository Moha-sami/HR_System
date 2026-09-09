using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.PreflightCopyShifts;

public record PreflightCopyShiftsQuery(
    int SiteId,
    DateOnly SourceDate,
    List<DateOnly>? TargetDates = null,
    List<DayOfWeek>? RecurringDays = null,
    int? WeekCount = null
) : IRequest<PreflightCopyShiftsResponseDto>;
