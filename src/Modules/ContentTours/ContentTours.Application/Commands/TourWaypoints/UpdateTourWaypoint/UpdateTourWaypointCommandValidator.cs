using FluentValidation;

namespace ContentTours.Application.Commands.TourWaypoints.UpdateTourWaypoint;

public sealed class UpdateTourWaypointCommandValidator : AbstractValidator<UpdateTourWaypointCommand>
{
    public UpdateTourWaypointCommandValidator()
    {
        RuleFor(x => x.TourId)
            .NotEmpty();

        RuleFor(x => x.WaypointId)
            .NotEmpty();

        // Plan rule #10: domain calls .Trim() on Name → validator MUST guard against null/empty
        // (parity with AddTourWaypointCommandValidator).
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description != null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90d, 90d);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180d, 180d);

        // PDF B1.1: reject exact (0, 0) — Gulf-of-Guinea sentinel, never a real Jordan waypoint.
        RuleFor(x => x)
            .Must(x => !(x.Latitude == 0d && x.Longitude == 0d))
            .WithMessage("Coordinates (0, 0) are not a valid waypoint location.")
            .OverridePropertyName("Location");

        RuleFor(x => x.StopDurationMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.StopDurationMinutes.HasValue);
    }
}
