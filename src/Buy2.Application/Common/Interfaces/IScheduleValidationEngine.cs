using Buy2.Application.DTOs;

namespace Buy2.Application.Common.Interfaces;

public interface IScheduleValidationEngine
{
    Task<PreFlightValidationResultDto> ValidateDraftAsync(List<DraftShiftDto> shifts);
}
