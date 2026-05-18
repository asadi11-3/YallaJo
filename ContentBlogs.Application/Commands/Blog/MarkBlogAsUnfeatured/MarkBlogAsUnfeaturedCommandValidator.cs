using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.MarkBlogAsUnfeatured;

public sealed class MarkBlogAsUnfeaturedCommandValidator : AbstractValidator<MarkBlogAsUnfeaturedCommand>
{
    public MarkBlogAsUnfeaturedCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv.Length > 0)
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
