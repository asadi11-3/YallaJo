using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentBlogs.Application.Commands.Blog.CreateBlog
{
    public sealed class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
    {
        public CreateBlogCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(500);
            RuleFor(x => x.Slug)
                .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(x => !string.IsNullOrEmpty(x.Slug))
                .MaximumLength(200);
            RuleFor(x => x.Summary)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.Summary));
            RuleFor(x => x.Content)
                .NotEmpty()
             .Must(BeAtLeast150CharsAfterStripHtml).WithMessage("Content must be at least 150 chars (excluding HTML).");
            RuleFor(x => x.PlaceId)
            .NotEqual(Guid.Empty)
            .When(x => x.PlaceId.HasValue);
            RuleFor(x => x.MetaTitle)
                .MaximumLength(60)
                .When(x => !string.IsNullOrEmpty(x.MetaTitle));
            RuleFor(x => x.MetaDescription)
                .MaximumLength(160)
                .When(x => !string.IsNullOrEmpty(x.MetaDescription));
            RuleFor(x => x.SourceLanguageCode)
                .NotEmpty()
                .Length(2)
                .Matches("^[a-z]{2}$");
        }

        private static bool BeAtLeast150CharsAfterStripHtml(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            var stripped = System.Text.RegularExpressions.Regex.Replace(content, "<.*?>", string.Empty);
            return stripped.Trim().Length >= 150;
        }
    }
}
