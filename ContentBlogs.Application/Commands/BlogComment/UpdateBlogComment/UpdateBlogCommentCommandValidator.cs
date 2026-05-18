using FluentValidation;

namespace ContentBlogs.Application.Commands.BlogComment.UpdateBlogComment;

public sealed class UpdateBlogCommentCommandValidator : AbstractValidator<UpdateBlogCommentCommand>
{
    public UpdateBlogCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull().WithMessage("RowVersion is required.")
            .Must(rv => rv is { Length: > 0 }).WithMessage("RowVersion cannot be empty.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(Domain.Entities.BlogComment.MaxContentLength);
    }
}
