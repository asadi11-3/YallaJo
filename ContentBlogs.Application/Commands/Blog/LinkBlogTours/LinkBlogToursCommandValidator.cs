using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.LinkBlogTours;

public sealed class LinkBlogToursCommandValidator : AbstractValidator<LinkBlogToursCommand>
{
    /// <summary>
    /// Per-request cap on the number of tour links submitted at once.  Note
    /// the per-blog cap (also 10) is enforced by the handler against the
    /// CURRENT linked-tours count, since a request that fits in 10 items may
    /// still tip the blog past its 10-tour ceiling.
    /// </summary>
    public const int MaxLinksPerRequest = 10;

    public LinkBlogToursCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv.Length > 0)
            .WithMessage("RowVersion is required for optimistic concurrency.");

        RuleFor(x => x.Tours)
            .NotNull()
            .Must(t => t.Count > 0)
                .WithMessage("At least one tour must be provided.")
            .Must(t => t.Count <= MaxLinksPerRequest)
                .WithMessage($"At most {MaxLinksPerRequest} tour links may be requested per call.")
            .Must(HaveDistinctTourIds)
                .WithMessage("Duplicate TourId in the request body.");

        RuleForEach(x => x.Tours).ChildRules(item =>
        {
            item.RuleFor(i => i.TourId).NotEqual(Guid.Empty);
            item.RuleFor(i => i.SortOrder!.Value)
                .GreaterThanOrEqualTo(0)
                .When(i => i.SortOrder.HasValue);
        });
    }

    private static bool HaveDistinctTourIds(IReadOnlyCollection<LinkBlogTourItem> items)
        => items.Select(i => i.TourId).Distinct().Count() == items.Count;
}
