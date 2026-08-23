using Buy2.Application.DTOs.Roles;
using Buy2.Domain.Enums;
using FluentValidation;

namespace Buy2.Application.Validators;

/// <summary>
/// Validator for PermissionScopeDto ensuring valid AccessScope enum value name and TargetIds invariant rules.
/// </summary>
public class PermissionScopeDtoValidator : AbstractValidator<PermissionScopeDto>
{
    public PermissionScopeDtoValidator()
    {
        RuleFor(x => x.ScopeType)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("ScopeType is required.")
            .Must(st => !string.IsNullOrWhiteSpace(st)).WithMessage("ScopeType cannot be empty.")
            .Must(st => Enum.TryParse<AccessScope>(st.Trim(), true, out var parsed) && Enum.IsDefined(typeof(AccessScope), parsed))
            .WithMessage("ScopeType must be a valid AccessScope enum value name.");

        RuleFor(x => x)
            .Must(scope =>
            {
                if (scope == null || string.IsNullOrWhiteSpace(scope.ScopeType)) return true;
                if (!Enum.TryParse<AccessScope>(scope.ScopeType.Trim(), true, out var parsedScopeType) || !Enum.IsDefined(typeof(AccessScope), parsedScopeType))
                {
                    return true;
                }

                if (parsedScopeType == AccessScope.All)
                {
                    return scope.TargetIds == null || scope.TargetIds.Count == 0;
                }
                else
                {
                    if (scope.TargetIds == null || scope.TargetIds.Count == 0)
                    {
                        return false;
                    }
                    return scope.TargetIds.All(id => id > 0);
                }
            })
            .WithMessage(scope =>
            {
                if (scope != null && !string.IsNullOrWhiteSpace(scope.ScopeType) &&
                    Enum.TryParse<AccessScope>(scope.ScopeType.Trim(), true, out var parsedScopeType) &&
                    parsedScopeType == AccessScope.All)
                {
                    return "TargetIds must be empty when ScopeType is All.";
                }
                return "TargetIds must not be empty and all IDs must be positive integers when ScopeType is not All.";
            });
    }
}
