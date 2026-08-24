using Buy2.Application.DTOs.Roles;
using MediatR;

namespace Buy2.Application.Features.Roles.DeleteRole;

public record ReassignUsersAndDeleteRoleCommand(
    int Id,
    ReassignUsersAndDeleteRoleDto Dto
) : IRequest<RoleDeletionResultDto>;
