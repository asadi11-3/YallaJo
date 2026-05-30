using FluentValidation;

namespace Social.Application.Commands.UpdateReviewReply;

internal sealed class UpdateReviewReplyCommandValidator : AbstractValidator<UpdateReviewReplyCommand>
{
    public UpdateReviewReplyCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.ReplyId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(1000);
    }
}
