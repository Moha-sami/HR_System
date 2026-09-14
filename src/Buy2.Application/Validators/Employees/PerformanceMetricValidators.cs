using Buy2.Application.DTOs.Employees;
using FluentValidation;

namespace Buy2.Application.Validators.Employees;

public class CreatePerformanceMetricDtoValidator : AbstractValidator<CreatePerformanceMetricDto>
{
    public CreatePerformanceMetricDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters.");

        // Display-only reference value shown next to the actual score.
        RuleFor(x => x.Target)
            .InclusiveBetween(0m, 100m)
            .WithMessage("Target must be between 0 and 100.");

        RuleFor(x => x.Weight)
            .GreaterThan(0m)
            .WithMessage("Weight must be greater than 0.")
            .LessThanOrEqualTo(100m)
            .WithMessage("Weight must not exceed 100.");
    }
}

public class UpdatePerformanceMetricDtoValidator : AbstractValidator<UpdatePerformanceMetricDto>
{
    public UpdatePerformanceMetricDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.Target)
            .InclusiveBetween(0m, 100m)
            .WithMessage("Target must be between 0 and 100.");

        RuleFor(x => x.Weight)
            .GreaterThan(0m)
            .WithMessage("Weight must be greater than 0.")
            .LessThanOrEqualTo(100m)
            .WithMessage("Weight must not exceed 100.");
    }
}

public class CreatePerformanceSubmissionDtoValidator : AbstractValidator<CreatePerformanceSubmissionDto>
{
    public CreatePerformanceSubmissionDtoValidator()
    {
        RuleFor(x => x.MetricId)
            .GreaterThan(0)
            .WithMessage("MetricId must be greater than 0.");

        RuleFor(x => x.Score)
            .InclusiveBetween(0m, 100m)
            .WithMessage("Score must be between 0 and 100.");

        RuleFor(x => x.Feedback)
            .MaximumLength(1000)
            .WithMessage("Feedback must not exceed 1000 characters.");
    }
}
