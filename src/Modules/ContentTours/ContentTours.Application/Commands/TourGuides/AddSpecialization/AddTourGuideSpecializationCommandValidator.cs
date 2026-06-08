using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.AddSpecialization;

public sealed class AddTourGuideSpecializationCommandValidator : AbstractValidator<AddTourGuideSpecializationCommand>
{
    public AddTourGuideSpecializationCommandValidator()
    {
        RuleFor(command => command.TourGuideId).NotEmpty();
        RuleFor(command => command.SpecializationId).NotEmpty();
    }
}
