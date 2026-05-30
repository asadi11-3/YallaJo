using System.Text.RegularExpressions;
using FluentValidation;

namespace ContentBlogs.Application.Queries.Blog.GetBlogBySlug;

public sealed class GetBlogBySlugQueryValidator : AbstractValidator<GetBlogBySlugQuery>
{
    private static readonly Regex SlugRegex =
        new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public GetBlogBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .Must(slug => SlugRegex.IsMatch(slug.Trim().ToLowerInvariant()))
                .WithMessage("Slug must be lowercase alphanumeric segments separated by '-'.")
            .MaximumLength(200);
    }
}
