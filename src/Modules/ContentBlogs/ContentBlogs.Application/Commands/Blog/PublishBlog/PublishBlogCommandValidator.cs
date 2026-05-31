using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.PublishBlog;

public sealed class PublishBlogCommandValidator : AbstractValidator<PublishBlogCommand>
{
    public PublishBlogCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
