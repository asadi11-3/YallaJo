using FluentValidation;

namespace ContentTours.Application.Commands.GuideApplication.Approve;

public sealed class ApproveGuideApplicationCommandValidator : AbstractValidator<ApproveGuideApplicationCommand>
{
    public ApproveGuideApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
