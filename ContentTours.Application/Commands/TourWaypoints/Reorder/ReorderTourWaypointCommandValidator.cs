using FluentValidation;

namespace ContentTours.Application.Features.TourWaypoints.Commands.Reorder;

public sealed class ReorderTourWaypointCommandValidator : AbstractValidator<ReorderTourWaypointCommand>
{
    public ReorderTourWaypointCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.WaypointId).NotEmpty();
        RuleFor(x => x.NewSortOrder).GreaterThanOrEqualTo(0);
    }
}
