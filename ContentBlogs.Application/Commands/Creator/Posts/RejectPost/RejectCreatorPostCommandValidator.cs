using FluentValidation;

namespace ContentBlogs.Application.Commands.Creator.Posts.RejectPost;

public sealed class RejectCreatorPostCommandValidator : AbstractValidator<RejectCreatorPostCommand>
{
    public RejectCreatorPostCommandValidator()
    {
        RuleFor(x => x.PostId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MinimumLength(50).WithMessage("Rejection reason must be at least 50 characters.")
            .MaximumLength(1000).WithMessage("Rejection reason must not exceed 1000 characters.");
    }
}
