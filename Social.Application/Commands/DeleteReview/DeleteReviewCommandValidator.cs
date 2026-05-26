using FluentValidation;

namespace Social.Application.Commands.DeleteReview;

internal sealed class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
    }
}
