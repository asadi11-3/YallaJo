using FluentValidation;

namespace Social.Application.Commands.AddHelpfulVote;

internal sealed class AddHelpfulVoteCommandValidator : AbstractValidator<AddHelpfulVoteCommand>
{
    public AddHelpfulVoteCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
