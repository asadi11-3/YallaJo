using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.Posts.CreatePost;

public sealed class CreateCreatorPostCommandValidator : AbstractValidator<CreateCreatorPostCommand>
{
    public CreateCreatorPostCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Excerpt)
            .NotEmpty().WithMessage("Excerpt is required.")
            .MinimumLength(100).WithMessage("Excerpt must be at least 100 characters.")
            .MaximumLength(500).WithMessage("Excerpt must not exceed 500 characters.");

        RuleFor(x => x.LanguageId)
            .NotEmpty().WithMessage("Language is required.");

        RuleFor(x => x.PostType)
            .IsInEnum().WithMessage("Invalid post type.");

        RuleFor(x => x.NicheIds)
            .Must(n => n is null || n.Count <= 3)
            .WithMessage("A post may have at most 3 niches.");

        RuleFor(x => x.FreeTags)
            .Must(t => t is null || t.Count <= 10)
            .WithMessage("A post may have at most 10 free tags.");
    }
}
