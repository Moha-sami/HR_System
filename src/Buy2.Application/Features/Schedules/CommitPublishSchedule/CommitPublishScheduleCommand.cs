using Buy2.Application.DTOs.Schedules;
using MediatR;

namespace Buy2.Application.Features.Schedules.CommitPublishSchedule;

public record CommitPublishScheduleCommand(
    int SiteId,
    List<DateOnly>? TargetDates = null,
    bool AllUnpublishedDays = false,
    List<int>? TargetRoleIds = null,
    Dictionary<int, PublishExceptionDecision>? ExceptionResolutions = null,
    string? OvertimeJustification = null
) : IRequest<CommitPublishScheduleResponseDto>;
