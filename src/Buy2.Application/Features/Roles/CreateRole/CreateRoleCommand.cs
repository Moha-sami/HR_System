using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace Buy2.Application.Features.Roles.CreateRole;

public record CreateRoleCommand(string RoleName, List<string> Permissions) : IRequest<int>;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, int>
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(IRepository<Role> roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var roleExists = await _roleRepository.AnyAsync(r => r.Name == request.RoleName, cancellationToken);

        if (roleExists)
        {
            throw new InvalidOperationException($"Role '{request.RoleName}' already exists.");
        }

        var role = new Role
        {
            Name = request.RoleName,
            PermissionsJson = JsonSerializer.Serialize(request.Permissions)
        };

        await _roleRepository.AddAsync(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}