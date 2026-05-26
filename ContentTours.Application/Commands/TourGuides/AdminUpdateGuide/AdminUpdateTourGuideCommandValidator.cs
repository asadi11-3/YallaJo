using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.AdminUpdateGuide;

public sealed class AdminUpdateTourGuideCommandValidator : AbstractValidator<AdminUpdateTourGuideCommand>
{
    public AdminUpdateTourGuideCommandValidator()
    {
        RuleFor(x => x.TourGuideId).NotEmpty();

        RuleFor(x => x.Bio)
            .MaximumLength(2000)
            .When(x => x.Bio is not null);

        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(0, 50)
            .When(x => x.YearsOfExperience.HasValue);

        RuleFor(x => x.MoTALicenseNumber)
            .MaximumLength(50)
            .When(x => x.MoTALicenseNumber is not null);
    }
}
