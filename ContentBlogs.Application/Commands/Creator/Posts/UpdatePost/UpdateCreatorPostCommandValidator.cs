using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.Posts.UpdatePost;

public sealed class UpdateCreatorPostCommandValidator : AbstractValidator<UpdateCreatorPostCommand>
{
    public UpdateCreatorPostCommandValidator()
    {
        RuleFor(x => x.PostId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Excerpt)
            .NotEmpty().WithMessage("Excerpt is required.")
            .MinimumLength(100).WithMessage("Excerpt must be at least 100 characters.")
            .MaximumLength(500).WithMessage("Excerpt must not exceed 500 characters.");
    }
}
