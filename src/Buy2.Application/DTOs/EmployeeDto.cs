namespace Buy2.Application.DTOs;

public record EmployeeDto(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    int JobRoleId,
    int RoleId,
    int SiteId
);
