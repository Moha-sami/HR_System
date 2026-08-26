using FluentValidation;

namespace Buy2.Application.Features.Employees.UpdateJobDetails.Validators;

public class UpdateJobDetailsDtoValidator : AbstractValidator<UpdateJobDetailsDto>
{
    public UpdateJobDetailsDtoValidator()
    {
        RuleFor(x => x.JobRoleId)
            .GreaterThan(0)
            .When(x => x.JobRoleId.HasValue)
            .WithMessage("JobRoleId must be greater than 0.");

        RuleFor(x => x.DirectManagerId)
            .GreaterThan(0)
            .When(x => x.DirectManagerId.HasValue)
            .WithMessage("DirectManagerId must be greater than 0.");

        RuleFor(x => x.ExperienceYears)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ExperienceYears.HasValue)
            .WithMessage("ExperienceYears must be greater than or equal to 0.");
    }
}
