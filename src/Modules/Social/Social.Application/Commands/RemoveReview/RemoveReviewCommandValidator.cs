using FluentValidation;

namespace Social.Application.Commands.RemoveReview;

internal sealed class RemoveReviewCommandValidator : AbstractValidator<RemoveReviewCommand>
{
    public RemoveReviewCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");

        RuleFor(x => x.Reason)
            .MaximumLength(1000)
            .When(x => x.Reason is not null);
    }
}
