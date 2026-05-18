using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.MarkBlogAsFeatured;

public sealed class MarkBlogAsFeaturedCommandValidator : AbstractValidator<MarkBlogAsFeaturedCommand>
{
    public MarkBlogAsFeaturedCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv.Length > 0)
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
