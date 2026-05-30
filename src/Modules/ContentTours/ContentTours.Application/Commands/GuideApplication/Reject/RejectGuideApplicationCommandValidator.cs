using FluentValidation;

namespace ContentTours.Application.Commands.GuideApplication.Reject;

public sealed class RejectGuideApplicationCommandValidator : AbstractValidator<RejectGuideApplicationCommand>
{
    public RejectGuideApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
