using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.RemoveLanguage;

public sealed class RemoveTourGuideLanguageCommandValidator : AbstractValidator<RemoveTourGuideLanguageCommand>
{
    public RemoveTourGuideLanguageCommandValidator()
    {
        RuleFor(command => command.TourGuideId).NotEmpty();
        RuleFor(command => command.LanguageId).NotEmpty();
    }
}
