using FluentValidation;

namespace ContentBlogs.Application.Commands.BlogComment.DeleteBlogComment;

public sealed class DeleteBlogCommentCommandValidator : AbstractValidator<DeleteBlogCommentCommand>
{
    public DeleteBlogCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEqual(Guid.Empty);
    }
}
