using FluentValidation;

namespace ContentTours.Application.Commands.TourGuides.UpdateProfile;

public sealed class UpdateTourGuideProfileCommandValidator : AbstractValidator<UpdateTourGuideProfileCommand>
{
    public UpdateTourGuideProfileCommandValidator()
    {
        RuleFor(command => command.TourGuideId).NotEmpty();
        RuleFor(command => command.CallerUserId).NotEmpty();
        RuleFor(command => command.Bio).NotEmpty().MaximumLength(2000);
        RuleFor(command => command.YearsOfExperience).InclusiveBetween(0, 80);
        RuleFor(command => command.MoTALicenseNumber).MaximumLength(128);
    }
}
