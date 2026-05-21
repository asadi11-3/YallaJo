using FluentValidation;

namespace Social.Application.Commands.AddReviewReply;

internal sealed class AddReviewReplyCommandValidator : AbstractValidator<AddReviewReplyCommand>
{
    public AddReviewReplyCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.ProviderUserId).NotEmpty();
        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(1000);
    }
}
