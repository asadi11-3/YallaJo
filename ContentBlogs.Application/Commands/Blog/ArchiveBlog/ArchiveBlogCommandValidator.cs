using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.ArchiveBlog;

public sealed class ArchiveBlogCommandValidator : AbstractValidator<ArchiveBlogCommand>
{
    public ArchiveBlogCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
    }
}
