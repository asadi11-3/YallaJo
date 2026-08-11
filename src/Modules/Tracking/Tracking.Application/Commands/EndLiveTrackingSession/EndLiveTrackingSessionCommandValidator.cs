using FluentValidation;

namespace Tracking.Application.Commands.EndLiveTrackingSession;

public sealed class EndLiveTrackingSessionCommandValidator : AbstractValidator<EndLiveTrackingSessionCommand>
{
    public EndLiveTrackingSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.EndedAt).NotEmpty();
    }
}
