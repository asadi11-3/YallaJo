using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.IncrementBlogViewCount;

public sealed class IncrementBlogViewCountCommandValidator
    : AbstractValidator<IncrementBlogViewCountCommand>
{
    public IncrementBlogViewCountCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
    }
}
