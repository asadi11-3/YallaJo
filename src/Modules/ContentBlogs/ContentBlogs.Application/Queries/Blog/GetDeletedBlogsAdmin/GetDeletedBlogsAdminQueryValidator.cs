using FluentValidation;

namespace ContentBlogs.Application.Queries.Blog.GetDeletedBlogsAdmin;

public sealed class GetDeletedBlogsAdminQueryValidator
    : AbstractValidator<GetDeletedBlogsAdminQuery>
{
    private static readonly string[] AllowedSortBy =
        ["deletedAt", "title", "updatedAt"];

    private static readonly string[] AllowedSortOrder =
        ["asc", "desc"];

    public GetDeletedBlogsAdminQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.PlaceId!.Value)
            .NotEqual(Guid.Empty)
            .When(x => x.PlaceId.HasValue);

        RuleFor(x => x.Status!.Value)
            .IsInEnum()
            .When(x => x.Status.HasValue);

        RuleFor(x => x.SortBy)
            .Must(s => s is null || AllowedSortBy.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be one of: deletedAt, title, updatedAt.");

        RuleFor(x => x.SortOrder)
            .Must(s => s is null || AllowedSortOrder.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortOrder must be 'asc' or 'desc'.");
    }
}
