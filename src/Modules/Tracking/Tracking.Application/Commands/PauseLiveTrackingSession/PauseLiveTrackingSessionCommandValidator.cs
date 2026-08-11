using FluentValidation;

namespace Tracking.Application.Commands.PauseLiveTrackingSession;

public sealed class PauseLiveTrackingSessionCommandValidator : AbstractValidator<PauseLiveTrackingSessionCommand>
{
    public PauseLiveTrackingSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.PausedAt).NotEmpty();
    }
}
