using Buy2.Application.DTOs.Points.DTOs;
using FluentValidation;

namespace Buy2.Application.Validators.Points;

public class ExecuteAutomationJobDtoValidator : AbstractValidator<ExecuteAutomationJobDto>
{
    public ExecuteAutomationJobDtoValidator()
    {
        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required.");

        RuleFor(x => x.PeriodStart)
            .LessThanOrEqualTo(x => x.PeriodEnd)
            .WithMessage("PeriodStart must be less than or equal to PeriodEnd.");

        RuleFor(x => x.TargetEmployeeIds)
            .Must(ids => ids == null || ids.Count == 0 || ids.All(id => id > 0))
            .WithMessage("All TargetEmployeeIds must be greater than 0.");

        RuleFor(x => x.TargetEmployeeIds)
            .Must(ids => ids == null || ids.Count == 0 || ids.Distinct().Count() == ids.Count)
            .WithMessage("TargetEmployeeIds must not contain duplicates.");
    }
}