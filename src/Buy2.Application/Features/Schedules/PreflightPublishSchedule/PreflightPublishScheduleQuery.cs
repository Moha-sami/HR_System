using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.PreflightPublishSchedule;

public record PreflightPublishScheduleQuery(
    int SiteId,
    List<DateOnly>? TargetDates = null,
    bool AllUnpublishedDays = false,
    List<int>? TargetRoleIds = null
) : IRequest<PreflightPublishScheduleResponseDto>;
