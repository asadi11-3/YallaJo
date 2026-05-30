using ContentBlogs.Domain.Entities;
using FluentValidation;

namespace ContentBlogs.Application.Commands.BlogComment.CreateBlogComment;

public sealed class CreateBlogCommentCommandValidator : AbstractValidator<CreateBlogCommentCommand>
{
    public CreateBlogCommentCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(Domain.Entities.BlogComment.MaxContentLength);

        RuleFor(x => x.ParentCommentId!.Value)
            .NotEqual(Guid.Empty)
            .When(x => x.ParentCommentId.HasValue);
    }
}
