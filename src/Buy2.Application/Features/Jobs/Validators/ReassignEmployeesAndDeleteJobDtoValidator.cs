using Buy2.Application.Features.Jobs.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.Jobs.Validators;

public class ReassignEmployeesAndDeleteJobDtoValidator : AbstractValidator<ReassignEmployeesAndDeleteJobDto>
{
    public ReassignEmployeesAndDeleteJobDtoValidator()
    {
        RuleFor(x => x.ReplacementJobId)
            .GreaterThan(0).WithMessage("ReplacementJobId must be greater than 0.");
    }
}
