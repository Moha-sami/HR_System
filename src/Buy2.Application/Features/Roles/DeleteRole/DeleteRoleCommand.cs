using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Roles.DeleteRole;

public record DeleteRoleCommand(int RoleId) : IRequest<bool>;

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
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
        {
            throw new InvalidOperationException($"Role '{request.RoleId}' not found.");
        }

        var isBound = await _employeeRepository.AnyAsync(e => e.RoleId == request.RoleId, cancellationToken);

        if (isBound)
        {
            throw new InvalidOperationException($"Role '{role.Name}' is assigned to employees and cannot be deleted.");
        }

        role.IsActive = false;
        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
