using Buy2.Application.DTOs.Roles;
using FluentValidation;

namespace Buy2.Application.Validators;

/// <summary>
/// Validator for ReassignUsersAndDeleteRoleDto enforcing role ID validity, reassignment criteria,
/// and prevention of self-assignment / identical replacement role IDs.
/// </summary>
public class ReassignUsersAndDeleteRoleDtoValidator : AbstractValidator<ReassignUsersAndDeleteRoleDto>
{
    public ReassignUsersAndDeleteRoleDtoValidator()
    {
        RuleFor(x => x.RoleId)
            .GreaterThan(0)
            .WithMessage("RoleId must be greater than 0.");

        RuleFor(x => x)
            .Must(dto => (dto.Reassignments != null && dto.Reassignments.Count > 0) || (dto.DefaultNewRoleId.HasValue && dto.DefaultNewRoleId.Value > 0))
            .WithMessage("Must specify either a non-empty reassignments list or a valid default new role ID.");

        When(x => x.DefaultNewRoleId.HasValue, () =>
        {
            RuleFor(x => x.DefaultNewRoleId!.Value)
                .GreaterThan(0)
                .WithMessage("Default new role ID must be greater than 0.");

            RuleFor(x => x.DefaultNewRoleId!.Value)
                .Must((dto, defaultNewRoleId) => defaultNewRoleId != dto.RoleId)
                .WithMessage("Default replacement role cannot be the same role being deleted.");
        });

        When(x => x.Reassignments != null, () =>
        {
            RuleFor(x => x.Reassignments!)
                .Must(list => list.Select(r => r.EmployeeId).Distinct().Count() == list.Count)
                .WithMessage("Reassignments list cannot contain duplicate employee entries.");

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

            RuleForEach(x => x.Reassignments!)
                .Must((dto, item) => item == null || item.NewRoleId != dto.RoleId)
                .WithMessage("Replacement role cannot be the same role being deleted.");
        });
    }
}
