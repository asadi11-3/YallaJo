using FluentValidation;

namespace Accounts.Application.Queries.Agency.GetMyInvitations;

public sealed class GetMyInvitationsQueryValidator : AbstractValidator<GetMyInvitationsQuery>
{
    public GetMyInvitationsQueryValidator()
    {
        RuleFor(x => x.Direction)
            .NotEmpty()
            .Must(d => d is "sent" or "received")
            .WithMessage("Direction must be 'sent' or 'received'.");
    }
}
