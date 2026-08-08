namespace Buy2.Application.DTOs;

public record PreFlightValidationResultDto(bool IsValid, List<string> Warnings, List<string> Errors);

