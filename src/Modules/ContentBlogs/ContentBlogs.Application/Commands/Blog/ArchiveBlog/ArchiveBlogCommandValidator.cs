using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.ArchiveBlog;

public sealed class ArchiveBlogCommandValidator : AbstractValidator<ArchiveBlogCommand>
{
    public ArchiveBlogCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
