using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.UnpublishBlog;

public sealed class UnpublishBlogCommandValidator : AbstractValidator<UnpublishBlogCommand>
{
    public UnpublishBlogCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
    }
}
