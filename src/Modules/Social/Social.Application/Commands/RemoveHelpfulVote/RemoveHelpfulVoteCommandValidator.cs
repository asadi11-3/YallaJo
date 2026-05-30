using FluentValidation;

namespace Social.Application.Commands.RemoveHelpfulVote;

internal sealed class RemoveHelpfulVoteCommandValidator : AbstractValidator<RemoveHelpfulVoteCommand>
{
    public RemoveHelpfulVoteCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
