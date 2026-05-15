using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.DeleteBlog;

public sealed class DeleteBlogCommandValidator : AbstractValidator<DeleteBlogCommand>
{
    public DeleteBlogCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
    }
}
