using MediatR;

namespace Buy2.Application.Features.Roles.CreateRole;

public record CreateRoleCommand(string RoleName, List<string> Permissions) : IRequest<int>;
