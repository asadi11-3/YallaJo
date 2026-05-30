using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.UnpublishBlog;

public sealed class UnpublishBlogCommandValidator : AbstractValidator<UnpublishBlogCommand>
{
    public UnpublishBlogCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv.Length > 0)
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
