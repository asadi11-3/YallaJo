using System.Text.RegularExpressions;
using ContentBlogs.Application.Commands.Blog.Common;
using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.UpdateBlog;

public sealed class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
{
    private static readonly Regex SlugRegex =
        new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public UpdateBlogCommandValidator()
    {
        RuleFor(x => x.BlogId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv.Length > 0)
            .WithMessage("RowVersion is required for optimistic concurrency.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .Must(slug => SlugRegex.IsMatch(slug.Trim().ToLowerInvariant()))
                .WithMessage("Slug must be lowercase alphanumeric segments separated by '-'.")
            .MaximumLength(200);

        RuleFor(x => x.Content)
            .NotEmpty()
            .Must(BeAtLeast150CharsAfterStripHtml)
                .WithMessage("Content must be at least 150 chars (excluding HTML).");

        RuleFor(x => x.Summary!)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Summary));

        RuleFor(x => x.MetaTitle!)
            .MaximumLength(60)
            .When(x => !string.IsNullOrEmpty(x.MetaTitle));

        RuleFor(x => x.MetaDescription!)
            .MaximumLength(160)
            .When(x => !string.IsNullOrEmpty(x.MetaDescription));

        RuleFor(x => x.PlaceId!.Value)
            .NotEqual(Guid.Empty)
            .When(x => x.PlaceId.HasValue);

        RuleFor(x => x.ReadTimeMinutes!.Value)
            .GreaterThan(0)
            .When(x => x.ReadTimeMinutes.HasValue);
    }

    private static bool BeAtLeast150CharsAfterStripHtml(string content) =>
        BlogContentTextHelper.StripHtml(content).Length >= 150;
}
