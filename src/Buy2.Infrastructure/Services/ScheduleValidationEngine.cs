using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs;

namespace Buy2.Infrastructure.Services;
public class ScheduleValidationEngine : IScheduleValidationEngine
{
    public Task<PreFlightValidationResultDto> ValidateDraftAsync(List<DraftShiftDto> shifts)
    {
        return Task.FromResult
            (
                new PreFlightValidationResultDto(
                        true,
                        new(),
                        new()
                    )
            );
    }
}