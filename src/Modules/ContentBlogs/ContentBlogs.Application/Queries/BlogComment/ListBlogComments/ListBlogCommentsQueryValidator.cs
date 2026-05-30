using FluentValidation;

namespace ContentBlogs.Application.Queries.BlogComment.ListBlogComments;

public sealed class ListBlogCommentsQueryValidator : AbstractValidator<ListBlogCommentsQuery>
{
    public ListBlogCommentsQueryValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
