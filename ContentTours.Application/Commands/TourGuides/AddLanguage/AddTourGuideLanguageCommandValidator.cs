using ContentTours.Domain.Entities;
using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.AddLanguage;

public sealed class AddTourGuideLanguageCommandValidator : AbstractValidator<AddTourGuideLanguageCommand>
{
    public AddTourGuideLanguageCommandValidator()
    {
        RuleFor(command => command.TourGuideId).NotEmpty();
        RuleFor(command => command.CallerUserId).NotEmpty();
        RuleFor(command => command.LanguageId).NotEmpty();
        RuleFor(command => command.Proficiency)
            .NotEmpty()
            .Must(TourGuide.IsValidProficiency)
            .WithMessage("Proficiency must be Native, Fluent, Conversational, or Basic.");
    }
}
