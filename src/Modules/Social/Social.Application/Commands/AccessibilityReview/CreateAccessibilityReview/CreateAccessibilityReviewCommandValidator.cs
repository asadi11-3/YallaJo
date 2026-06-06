using FluentValidation;

namespace Social.Application.Commands.AccessibilityReview.CreateAccessibilityReview;

public sealed class CreateAccessibilityReviewCommandValidator : AbstractValidator<CreateAccessibilityReviewCommand>
{
    public CreateAccessibilityReviewCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.Rating)
            .GreaterThanOrEqualTo(0.5m)
            .LessThanOrEqualTo(5.0m)
            .Must(r => (r * 2) % 1 == 0)
            .WithMessage("Rating must be in 0.5 increments.");
        RuleFor(x => x.Content).NotEmpty().MinimumLength(10).MaximumLength(5000);
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.FeatureTypesCsv)
            .NotEmpty()
            .MaximumLength(500)
            .WithMessage("At least one accessibility feature type is required.");
    }
}
