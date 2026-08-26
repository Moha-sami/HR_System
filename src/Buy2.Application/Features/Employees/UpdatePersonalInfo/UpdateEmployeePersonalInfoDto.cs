using Buy2.Domain.Enums;

namespace Buy2.Application.Features.Employees.UpdatePersonalInfo;

public record UpdateEmployeePersonalInfoDto(
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    DateTimeOffset? Birthdate = null,
    Gender? Gender = null,
    string? Email = null
);
