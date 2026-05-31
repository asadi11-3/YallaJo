using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.RestoreBlog;

public sealed class RestoreBlogCommandValidator : AbstractValidator<RestoreBlogCommand>
{
    public RestoreBlogCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
