using FluentValidation;

namespace Social.Application.Commands.DeleteReviewReply;

internal sealed class DeleteReviewReplyCommandValidator : AbstractValidator<DeleteReviewReplyCommand>
{
    public DeleteReviewReplyCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.ReplyId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
    }
}
