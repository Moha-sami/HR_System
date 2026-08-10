using MediatR;

namespace Buy2.Application.Features.Roles.DeleteRole;

public record DeleteRoleCommand(int RoleId) : IRequest<bool>;
