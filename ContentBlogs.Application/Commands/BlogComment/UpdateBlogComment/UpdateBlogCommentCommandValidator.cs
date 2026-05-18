using FluentValidation;

namespace ContentBlogs.Application.Commands.BlogComment.UpdateBlogComment;

public sealed class UpdateBlogCommentCommandValidator : AbstractValidator<UpdateBlogCommentCommand>
{
    public UpdateBlogCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEqual(Guid.Empty);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(Domain.Entities.BlogComment.MaxContentLength);
    }
}
