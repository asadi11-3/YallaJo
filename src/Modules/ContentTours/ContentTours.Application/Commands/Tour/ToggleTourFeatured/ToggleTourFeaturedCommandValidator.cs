using FluentValidation;

namespace ContentTours.Application.Commands.Tour.ToggleTourFeatured;

public sealed class ToggleTourFeaturedCommandValidator : AbstractValidator<ToggleTourFeaturedCommand>
{
    public ToggleTourFeaturedCommandValidator()
    {
        RuleFor(x => x.TourId).NotEqual(Guid.Empty);
    }
}
