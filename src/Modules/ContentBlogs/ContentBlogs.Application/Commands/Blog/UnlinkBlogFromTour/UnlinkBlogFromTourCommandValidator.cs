using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.UnlinkBlogFromTour;

public sealed class UnlinkBlogFromTourCommandValidator
    : AbstractValidator<UnlinkBlogFromTourCommand>
{
    public UnlinkBlogFromTourCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv.Length > 0)
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
