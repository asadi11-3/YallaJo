using FluentValidation;

namespace Social.Application.Commands.SubmitReport;

internal sealed class SubmitReportCommandValidator : AbstractValidator<SubmitReportCommand>
{
    public SubmitReportCommandValidator()
    {
        RuleFor(x => x.ReporterUserId).NotEmpty();
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(500);
    }
}
