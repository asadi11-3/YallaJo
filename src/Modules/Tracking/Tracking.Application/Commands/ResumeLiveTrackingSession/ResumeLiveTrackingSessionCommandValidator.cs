using FluentValidation;

namespace Tracking.Application.Commands.ResumeLiveTrackingSession;

public sealed class ResumeLiveTrackingSessionCommandValidator : AbstractValidator<ResumeLiveTrackingSessionCommand>
{
    public ResumeLiveTrackingSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ResumedAt).NotEmpty();
    }
}
