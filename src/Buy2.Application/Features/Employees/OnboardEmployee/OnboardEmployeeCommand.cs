using MediatR;

namespace Buy2.Application.Features.Employees.OnboardEmployee;
public record OnboardEmployeeCommand(
        string FirstName,
        string LastName,
        string Email,
        int JobRoleId,
        int SiteId
    ) : IRequest<int>;
