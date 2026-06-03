using ContentBlogs.Application.Commands.Blog.Common;
using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.UpsertBlogTranslation;

public sealed class UpsertBlogTranslationCommandValidator
    : AbstractValidator<UpsertBlogTranslationCommand>
{
    public UpsertBlogTranslationCommandValidator()
    {
        RuleFor(x => x.LanguageCode)
            .NotEmpty().WithMessage("Language code is required.")
            .Length(2)
            .Matches("^[a-z]{2}$").WithMessage("Language code must be a 2-letter lowercase code.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500);

        // Mirror the blog content rule: at least 150 chars excluding HTML.
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .Must(c => BlogContentTextHelper.StripHtml(c).Length >= 150)
                .WithMessage("Content must be at least 150 chars (excluding HTML).");

        RuleFor(x => x.Summary!)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Summary));
    }
}
