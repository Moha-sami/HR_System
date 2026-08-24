using Buy2.Application.DTOs.Roles;
using FluentValidation;

namespace Buy2.Application.Features.Roles.DeleteRole;

public class ReassignUsersAndDeleteRoleDtoValidator : AbstractValidator<ReassignUsersAndDeleteRoleDto>
{
    public ReassignUsersAndDeleteRoleDtoValidator()
    {
        RuleFor(x => x)
            .Must(dto => (dto.Reassignments != null && dto.Reassignments.Count > 0) || (dto.DefaultNewRoleId.HasValue && dto.DefaultNewRoleId.Value > 0))
            .WithMessage("Must specify either a non-empty reassignments list or a valid default new role ID.");

        When(x => x.DefaultNewRoleId.HasValue, () =>
        {
            RuleFor(x => x.DefaultNewRoleId!.Value)
                .GreaterThan(0)
                .WithMessage("Default new role ID must be greater than 0.");
        });

        When(x => x.Reassignments != null, () =>
        {
            RuleForEach(x => x.Reassignments!)
                .ChildRules(item =>
                {
                    item.RuleFor(r => r.EmployeeId)
                        .GreaterThan(0)
                        .WithMessage("Employee ID must be greater than 0.");

                    item.RuleFor(r => r.NewRoleId)
                        .GreaterThan(0)
                        .WithMessage("New role ID must be greater than 0.");
                });
        });
    }
}
