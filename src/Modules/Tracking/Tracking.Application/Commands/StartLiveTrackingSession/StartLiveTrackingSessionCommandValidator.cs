using FluentValidation;

namespace Tracking.Application.Commands.StartLiveTrackingSession;

public sealed class StartLiveTrackingSessionCommandValidator : AbstractValidator<StartLiveTrackingSessionCommand>
{
    public StartLiveTrackingSessionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.TourBookingId).NotEmpty();
        RuleFor(x => x.TourGuideId).NotEmpty();
        RuleFor(x => x.StartedAt).NotEmpty();

        RuleForEach(x => x.WaypointIds)
            .NotEmpty()
            .When(x => x.WaypointIds is { Count: > 0 });
    }
}
