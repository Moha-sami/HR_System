using FluentValidation;

namespace Buy2.Application.Features.Employees.UpdatePersonalInfo.Validators;

public class UpdateEmployeePersonalInfoDtoValidator : AbstractValidator<UpdateEmployeePersonalInfoDto>
{
    public UpdateEmployeePersonalInfoDtoValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Birthdate)
            .LessThan(DateTimeOffset.UtcNow)
            .When(x => x.Birthdate.HasValue)
            .WithMessage("Birthdate must be in the past.");

        RuleFor(x => x.Gender)
            .IsInEnum()
            .When(x => x.Gender.HasValue)
            .WithMessage("Gender must be a valid enum value.");

        RuleFor(x => x.FirstName)
            .MaximumLength(50)
            .When(x => x.FirstName != null);

        RuleFor(x => x.LastName)
            .MaximumLength(50)
            .When(x => x.LastName != null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => x.PhoneNumber != null);
    }
}
