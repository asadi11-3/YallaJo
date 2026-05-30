using FluentValidation;

namespace ContentTours.Application.Commands.TourWaypoints.ReorderTourWaypoints;

public sealed class ReorderTourWaypointsCommandValidator : AbstractValidator<ReorderTourWaypointsCommand>
{
    public ReorderTourWaypointsCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.WaypointIds).NotEmpty();
        RuleForEach(x => x.WaypointIds).NotEqual(Guid.Empty);
    }
}
