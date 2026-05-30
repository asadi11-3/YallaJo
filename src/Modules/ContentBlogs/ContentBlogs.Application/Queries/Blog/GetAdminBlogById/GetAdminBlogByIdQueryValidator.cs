using FluentValidation;

namespace ContentBlogs.Application.Queries.Blog.GetAdminBlogById;

public sealed class GetAdminBlogByIdQueryValidator : AbstractValidator<GetAdminBlogByIdQuery>
{
    public GetAdminBlogByIdQueryValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
    }
}
