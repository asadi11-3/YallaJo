using FluentValidation;

namespace ContentTours.Application.Commands.GuideTourOffering.SuspendGuideOffering;

public sealed class SuspendGuideOfferingCommandValidator : AbstractValidator<SuspendGuideOfferingCommand>
{
    public SuspendGuideOfferingCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.TourGuideId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
