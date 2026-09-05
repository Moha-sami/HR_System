using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buy2.Application.Features.Roles.DeleteRole;

public record ReassignUsersAndDeleteRoleCommand(
    int Id,
    ReassignUsersAndDeleteRoleDto Dto
) : IRequest<RoleDeletionResultDto>;

public class ReassignUsersAndDeleteRoleCommandHandler : IRequestHandler<ReassignUsersAndDeleteRoleCommand, RoleDeletionResultDto>
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReassignUsersAndDeleteRoleCommandHandler(
        IRepository<Role> roleRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RoleDeletionResultDto> Handle(ReassignUsersAndDeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.Query(asNoTracking: false)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role == null)
        {
            throw new KeyNotFoundException($"Role with ID {request.Id} was not found.");
        }

        if (role.IsSystemRole)
        {
            throw new InvalidOperationException("System roles cannot be deleted.");
        }

        var assignedEmployees = await _employeeRepository.Query(asNoTracking: false)
            .IgnoreQueryFilters()
            .Where(e => !e.IsDeleted && e.RoleId == request.Id)
            .ToListAsync(cancellationToken);

        var employeeTargetRoles = new Dictionary<int, int>();

        if (assignedEmployees.Count > 0)
        {
            var reassignmentMap = BuildReassignmentMap(request.Dto.Reassignments);

            foreach (var employee in assignedEmployees)
            {
                if (reassignmentMap.TryGetValue(employee.Id, out var newRoleId))
                {
                    employeeTargetRoles[employee.Id] = newRoleId;
                }
                else if (request.Dto.DefaultNewRoleId.HasValue)
                {
                    employeeTargetRoles[employee.Id] = request.Dto.DefaultNewRoleId.Value;
                }
                else
                {
                    throw new ArgumentException("All assigned employees must be mapped to a replacement role before deletion.");
                }
            }

            var distinctReplacementRoleIds = employeeTargetRoles.Values.Distinct().ToList();

            if (distinctReplacementRoleIds.Any(rId => rId == request.Id))
            {
                throw new ArgumentException("Invalid replacement role specified.");
            }

            var validActiveRoleIds = await _roleRepository.Query()
                .Where(r => r.IsActive && distinctReplacementRoleIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (validActiveRoleIds.Count != distinctReplacementRoleIds.Count)
            {
                throw new ArgumentException("Invalid replacement role specified.");
            }
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var employee in assignedEmployees)
            {
                employee.RoleId = employeeTargetRoles[employee.Id];
                _employeeRepository.Update(employee);
            }

            role.IsActive = false;
            role.UpdatedAt = DateTimeOffset.UtcNow;
            _roleRepository.Update(role);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RoleDeletionResultDto(true, request.Id, assignedEmployees.Count, "Role deleted and users reassigned successfully.");
        }, cancellationToken);
    }

    private static Dictionary<int, int> BuildReassignmentMap(List<EmployeeReassignmentDto>? reassignments)
    {
        var map = new Dictionary<int, int>();
        if (reassignments != null)
        {
            foreach (var item in reassignments)
            {
                map[item.EmployeeId] = item.NewRoleId;
            }
        }
        return map;
    }
}
