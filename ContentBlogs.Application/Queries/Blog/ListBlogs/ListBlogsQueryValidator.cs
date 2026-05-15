using FluentValidation;

namespace ContentBlogs.Application.Queries.Blog.ListBlogs;

public sealed class ListBlogsQueryValidator : AbstractValidator<ListBlogsQuery>
{
    public ListBlogsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.PlaceId!.Value)
            .NotEqual(Guid.Empty)
            .When(x => x.PlaceId.HasValue);
    }
}
