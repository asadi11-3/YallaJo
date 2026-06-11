using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.RemoveSpecialization;

public sealed class RemoveTourGuideSpecializationCommandValidator : AbstractValidator<RemoveTourGuideSpecializationCommand>
{
    public RemoveTourGuideSpecializationCommandValidator()
    {
        RuleFor(command => command.TourGuideId).NotEmpty();
        RuleFor(command => command.SpecializationId).NotEmpty();
    }
}
