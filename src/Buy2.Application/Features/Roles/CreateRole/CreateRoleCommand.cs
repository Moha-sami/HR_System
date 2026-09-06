using Buy2.Application.Common.Interfaces;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Buy2.Application.Features.Roles.CreateRole;

public record CreateRoleResult(bool Success, bool IsConflict, RoleDetailsDto? CreatedRole, string? ErrorMessage);

public record CreateRoleCommand(CreateRoleDto Dto) : IRequest<CreateRoleResult>;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleResult>
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(IRepository<Role> roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateRoleResult> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Dto.Name.Trim();
        var normalizedName = trimmedName.ToLower();

        var roleExists = await _roleRepository.Query()
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Name.ToLower() == normalizedName, cancellationToken);

        if (roleExists)
        {
            return new CreateRoleResult(false, true, null, $"Role with name '{request.Dto.Name}' already exists.");
        }

        var description = request.Dto.Description?.Trim();
        var permissions = request.Dto.Permissions ?? new List<ModulePermissionDto>();
        var permissionsJson = JsonSerializer.Serialize(permissions);

        var role = new Role
        {
            Name = trimmedName,
            Description = description,
            IsSystemRole = false,
            IsActive = true,
            PermissionsJson = permissionsJson,
            CreatedAt = DateTime.UtcNow
        };

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var roleDetailsDto = new RoleDetailsDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.IsActive,
            0,
            role.CreatedAt,
            role.UpdatedAt,
            permissions
        );

        return new CreateRoleResult(true, false, roleDetailsDto, null);
    }
}