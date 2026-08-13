using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs;
using MediatR;

namespace Buy2.Application.Features.Schedules.ValidateDraft;

public record ValidateScheduleDraftCommand
    (
        List<DraftShiftDto> Shifts
    ) : IRequest<PreFlightValidationResultDto>;

public class ValidateScheduleDraftCommandHandler : IRequestHandler<ValidateScheduleDraftCommand, PreFlightValidationResultDto>
{
    private readonly IScheduleValidationEngine _scheduleValidationEngine;

    public ValidateScheduleDraftCommandHandler(IScheduleValidationEngine scheduleValidationEngine)
    {
        _scheduleValidationEngine = scheduleValidationEngine;
    }

    public async Task<PreFlightValidationResultDto> Handle(ValidateScheduleDraftCommand command, CancellationToken cancellationToken)
    {
        var result = await _scheduleValidationEngine.ValidateDraftAsync(command.Shifts);

        return result;
    }
}