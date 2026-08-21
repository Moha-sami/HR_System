namespace Buy2.Application.Features.Employees.UpdatePersonalInfo;

public record UpdateEmployeePersonalInfoDto(
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    DateTimeOffset? DateOfBirth = null,
    string? Address = null,
    string? EmergencyContact = null,
    string? NationalId = null
);
