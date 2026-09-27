using FluentValidation;

namespace Buy2.Application.Features.Requests.DeleteRequestType;

public class DeleteRequestTypeCommandValidator : AbstractValidator<DeleteRequestTypeCommand>
{
    public DeleteRequestTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Request type ID must be greater than 0.");
    }
}
