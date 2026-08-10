using FluentValidation;

namespace Buy2.Application.Features.Roles.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .WithMessage("Role name is required.")
            .MaximumLength(100)
            .WithMessage("Role name must not exceed 100 characters.");

        RuleFor(x => x.Permissions)
            .NotNull()
            .WithMessage("Permissions are required.")
            .Must(x => x.Count > 0)
            .WithMessage("At least one permission is required.");
    }
}
