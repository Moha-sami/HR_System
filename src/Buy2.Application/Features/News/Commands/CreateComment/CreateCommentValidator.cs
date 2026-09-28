using FluentValidation;

namespace Buy2.Application.Features.News.Commands.CreateComment;

public class CreateCommentValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0)
            .WithMessage("Post ID must be greater than 0.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Comment content is required.")
            .MaximumLength(2000)
            .WithMessage("Comment content cannot exceed 2000 characters.");

        When(x => x.ParentCommentId.HasValue, () =>
        {
            RuleFor(x => x.ParentCommentId!.Value)
                .GreaterThan(0)
                .WithMessage("Parent comment ID must be greater than 0.");
        });
    }
}
