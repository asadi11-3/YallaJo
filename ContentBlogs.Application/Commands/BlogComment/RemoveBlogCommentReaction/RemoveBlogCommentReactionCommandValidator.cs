using FluentValidation;

namespace ContentBlogs.Application.Commands.BlogComment.RemoveBlogCommentReaction;

public sealed class RemoveBlogCommentReactionCommandValidator
    : AbstractValidator<RemoveBlogCommentReactionCommand>
{
    public RemoveBlogCommentReactionCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEqual(Guid.Empty);
    }
}
