using FluentValidation;

namespace ContentBlogs.Application.Commands.BlogComment.AddOrReplaceBlogCommentReaction;

public sealed class AddOrReplaceBlogCommentReactionCommandValidator
    : AbstractValidator<AddOrReplaceBlogCommentReactionCommand>
{
    public AddOrReplaceBlogCommentReactionCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEqual(Guid.Empty);
        RuleFor(x => x.ReactionType).IsInEnum();
    }
}
