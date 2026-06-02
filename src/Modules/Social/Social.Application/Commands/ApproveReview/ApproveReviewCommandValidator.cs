using FluentValidation;

namespace Social.Application.Commands.ApproveReview;

internal sealed class ApproveReviewCommandValidator : AbstractValidator<ApproveReviewCommand>
{
    public ApproveReviewCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
