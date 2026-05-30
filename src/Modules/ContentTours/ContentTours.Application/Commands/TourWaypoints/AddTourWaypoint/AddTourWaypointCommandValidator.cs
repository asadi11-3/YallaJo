using FluentValidation;

namespace ContentTours.Application.Commands.TourWaypoints.AddTourWaypoint;

public sealed class AddTourWaypointCommandValidator : AbstractValidator<AddTourWaypointCommand>
{
    public AddTourWaypointCommandValidator()
    {
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

        RuleFor(x => x.DurationMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DurationMinutes.HasValue);

        RuleFor(x => x.WaypointType)
            .IsInEnum();
    }
}
