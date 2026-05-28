using FluentValidation;

namespace Social.Application.Commands.RemoveReview;

internal sealed class RemoveReviewCommandValidator : AbstractValidator<RemoveReviewCommand>
{
    public RemoveReviewCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.Reason)
            .MaximumLength(1000)
            .When(x => x.Reason is not null);
    }
}
