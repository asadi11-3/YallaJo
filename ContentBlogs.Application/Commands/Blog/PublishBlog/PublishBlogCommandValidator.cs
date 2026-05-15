using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.PublishBlog;

public sealed class PublishBlogCommandValidator : AbstractValidator<PublishBlogCommand>
{
    public PublishBlogCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
    }
}
