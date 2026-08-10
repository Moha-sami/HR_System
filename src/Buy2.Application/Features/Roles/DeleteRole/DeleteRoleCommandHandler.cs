using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Roles.DeleteRole;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoleCommandHandler(
        IRepository<Role> roleRepository,
        IRepository<Employee> employeeRepository,
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId);

        if (role is null)
            throw new InvalidOperationException($"Role with id '{request.RoleId}' does not exist.");

        var employees = await _employeeRepository.GetAllAsync();
        var hasAssignedEmployees = employees.Any(e => e.RoleId == request.RoleId);

        if (hasAssignedEmployees)
            throw new InvalidOperationException($"Role '{role.Name}' is assigned to one or more employees and cannot be deleted.");

        role.IsActive = false;
        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
