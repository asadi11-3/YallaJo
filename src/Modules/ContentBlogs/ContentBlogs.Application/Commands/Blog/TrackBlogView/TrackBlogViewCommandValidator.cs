using FluentValidation;

namespace ContentBlogs.Application.Commands.Blog.TrackBlogView;

public sealed class TrackBlogViewCommandValidator : AbstractValidator<TrackBlogViewCommand>
{
    public TrackBlogViewCommandValidator()
    {
        RuleFor(x => x.BlogId).NotEqual(Guid.Empty);
        RuleFor(x => x.ViewerId).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ViewerKind).IsInEnum();
    }
}
