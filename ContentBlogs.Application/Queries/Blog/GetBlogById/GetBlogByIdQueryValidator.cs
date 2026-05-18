using FluentValidation;

namespace ContentBlogs.Application.Queries.Blog.GetBlogById;

public sealed class GetBlogByIdQueryValidator : AbstractValidator<GetBlogByIdQuery>
{
    public GetBlogByIdQueryValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
    }
}
