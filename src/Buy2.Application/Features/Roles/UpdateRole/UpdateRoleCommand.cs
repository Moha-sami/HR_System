using Buy2.Application.Common.Interfaces;
using Buy2.Application.Common.Specifications;
using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Entities;
using MediatR;
using System.Text.Json;

namespace Buy2.Application.Features.Roles.UpdateRole;

public record UpdateRoleResult(bool Success, bool IsNotFound, bool IsForbidden, bool IsConflict, RoleDetailsDto? UpdatedRole, string? ErrorMessage);

public record UpdateRoleCommand(int Id, UpdateRoleDto Dto) : IRequest<UpdateRoleResult>;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, UpdateRoleResult>
{
    private readonly IRepository<Role> _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoleCommandHandler(IRepository<Role> roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateRoleResult> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var roleSpec = new Specification<Role>()
            .IgnoreFilters()
            .Include(nameof(Role.Employees))
            .Where(r => r.Id == request.Id)
            .AsTracked();
        var role = await _roleRepository.FirstOrDefaultAsync(roleSpec, cancellationToken);

        if (role == null)
        {
            return new UpdateRoleResult(false, true, false, false, null, $"Role with ID {request.Id} was not found.");
        }

        var trimmedName = request.Dto.Name.Trim();
        var normalizedName = trimmedName.ToLower();

        if (role.IsSystemRole)
        {
            if (role.Name.Trim().ToLower() != normalizedName || !request.Dto.IsActive)
            {
                return new UpdateRoleResult(false, false, true, false, null, "Core system role name and active status cannot be modified.");
            }
        }

        var duplicateSpec = new Specification<Role>()
            .IgnoreFilters()
            .Where(r => r.Id != request.Id && r.Name.ToLower() == normalizedName);
        var isDuplicateName = await _roleRepository.CountAsync(duplicateSpec, cancellationToken) > 0;

        if (isDuplicateName)
        {
            return new UpdateRoleResult(false, false, false, true, null, $"Role with name '{request.Dto.Name}' already exists.");
        }

        var permissions = request.Dto.Permissions ?? new List<ModulePermissionDto>();

        role.Name = trimmedName;
        role.Description = request.Dto.Description?.Trim();
        role.IsActive = request.Dto.IsActive;
        role.PermissionsJson = JsonSerializer.Serialize(permissions);
        role.UpdatedAt = DateTimeOffset.UtcNow;

        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var assignedEmployeesCount = role.Employees?.Count(e => !e.IsDeleted) ?? 0;
        var roleDetailsDto = MapToRoleDetailsDto(role, assignedEmployeesCount, permissions);

        return new UpdateRoleResult(true, false, false, false, roleDetailsDto, null);
    }

    private static RoleDetailsDto MapToRoleDetailsDto(Role role, int assignedCount, List<ModulePermissionDto> permissions)
    {
        return new RoleDetailsDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.IsActive,
            assignedCount,
            role.CreatedAt,
            role.UpdatedAt,
            permissions
        );
    }
}
