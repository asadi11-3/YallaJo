using FluentValidation;
using Social.Domain.Enums;

namespace Social.Application.Commands.CreateReview;

internal sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.Rating)
            .InclusiveBetween(1.0m, 5.0m)
            .Must(r => (r * 2) % 1 == 0)
            .WithMessage("Rating must be between 1.0 and 5.0 in 0.5 increments.");
        RuleFor(x => x.Title)
            .MaximumLength(150)
            .When(x => x.Title is not null);
        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(2000);
    }
}
