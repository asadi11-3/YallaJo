using FluentValidation;

namespace ContentTours.Application.Features.TourWaypoints.Commands.Add;

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
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);

        RuleFor(x => x.DurationMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DurationMinutes.HasValue);

        RuleFor(x => x.WaypointType)
            .IsInEnum();
    }
}
