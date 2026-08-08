namespace Buy2.Application.DTOs.Employees;

public record OnboardEmployeeDto(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    int JobRoleId,
    int RoleId,
    int SiteId
);
