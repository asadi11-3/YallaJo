using FluentValidation;

namespace Social.Application.Commands.ResolveReport;

internal sealed class ResolveReportCommandValidator : AbstractValidator<ResolveReportCommand>
{
    public ResolveReportCommandValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);
    }
}
