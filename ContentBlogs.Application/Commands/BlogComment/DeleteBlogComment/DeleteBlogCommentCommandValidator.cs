using FluentValidation;

namespace ContentBlogs.Application.Commands.BlogComment.DeleteBlogComment;

public sealed class DeleteBlogCommentCommandValidator : AbstractValidator<DeleteBlogCommentCommand>
{
    public DeleteBlogCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull().WithMessage("RowVersion is required.")
            .Must(rv => rv is { Length: > 0 }).WithMessage("RowVersion cannot be empty.");
    }
}
