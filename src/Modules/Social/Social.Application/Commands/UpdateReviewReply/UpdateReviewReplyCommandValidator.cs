using FluentValidation;

namespace Social.Application.Commands.UpdateReviewReply;

internal sealed class UpdateReviewReplyCommandValidator : AbstractValidator<UpdateReviewReplyCommand>
{
    public UpdateReviewReplyCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.ReplyId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(5)
            .MaximumLength(1000);
    }
}
