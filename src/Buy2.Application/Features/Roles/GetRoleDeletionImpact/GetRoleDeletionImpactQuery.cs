using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Roles.GetRoleDeletionImpact;

public record GetRoleDeletionImpactQuery(int Id) : IRequest<RoleDeletionImpactDto?>;

public class GetRoleDeletionImpactQueryHandler : IRequestHandler<GetRoleDeletionImpactQuery, RoleDeletionImpactDto?>
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public GetRoleDeletionImpactQueryHandler(
        IRepository<Role> roleRepository,
        IRepository<Employee> employeeRepository)
    {
        _roleRepository = roleRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<RoleDeletionImpactDto?> Handle(GetRoleDeletionImpactQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.Query()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role is null)
        {
            return null;
        }

        var employees = await _employeeRepository.Query()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(e => e.JobRole)
            .Include(e => e.Site)
            .Where(e => !e.IsDeleted && e.RoleId == request.Id)
            .ToListAsync(cancellationToken);

        var affectedEmployees = employees.Select(e =>
        {
            var fullName = $"{e.FirstName} {e.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = "N/A";
            }

            var jobRoleTitle = e.JobRole?.Title ?? "N/A";
            if (string.IsNullOrWhiteSpace(jobRoleTitle))
            {
                jobRoleTitle = "N/A";
            }

            var siteName = e.Site?.SiteName ?? "N/A";
            if (string.IsNullOrWhiteSpace(siteName))
            {
                siteName = "N/A";
            }

            var employeeCode = string.IsNullOrWhiteSpace(e.EmployeeCode) ? "N/A" : e.EmployeeCode;
            var email = string.IsNullOrWhiteSpace(e.Email) ? "N/A" : e.Email;

            return new AffectedEmployeeDto(
                e.Id,
                employeeCode,
                fullName,
                email,
                jobRoleTitle,
                siteName
            );
        }).ToList();

        var assignedEmployeesCount = employees.Count;
        var canDeleteDirectly = !role.IsSystemRole && assignedEmployeesCount == 0;

        return new RoleDeletionImpactDto(
            role.Id,
            role.Name,
            role.IsSystemRole,
            canDeleteDirectly,
            assignedEmployeesCount,
            affectedEmployees
        );
    }
}
