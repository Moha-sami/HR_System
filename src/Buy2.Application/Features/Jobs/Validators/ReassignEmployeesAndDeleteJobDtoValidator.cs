using Buy2.Application.Features.Jobs.DTOs;
using FluentValidation;

namespace Buy2.Application.Features.Jobs.Validators;

public class ReassignEmployeesAndDeleteJobDtoValidator : AbstractValidator<ReassignEmployeesAndDeleteJobDto>
{
    public ReassignEmployeesAndDeleteJobDtoValidator()
    {
        RuleFor(x => x.DefaultReplacementJobId)
            .GreaterThan(0).When(x => x.DefaultReplacementJobId.HasValue)
            .WithMessage("DefaultReplacementJobId must be greater than 0.");

        RuleFor(x => x.ReplacementJobId)
            .GreaterThan(0).When(x => x.ReplacementJobId.HasValue)
            .WithMessage("ReplacementJobId must be greater than 0.");

        RuleForEach(x => x.Reassignments).ChildRules(r =>
        {
            r.RuleFor(i => i.EmployeeId)
                .GreaterThan(0).WithMessage("EmployeeId must be greater than 0.");
            r.RuleFor(i => i.NewJobId)
                .GreaterThan(0).WithMessage("NewJobId must be greater than 0.");
        }).When(x => x.Reassignments != null);
    }
}
