using FluentValidation;

namespace Messaging.Application.Commands.ResolveSupportTicket;

internal sealed class ResolveSupportTicketCommandValidator : AbstractValidator<ResolveSupportTicketCommand>
{
    public ResolveSupportTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.ResolvedByUserId).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");

        RuleFor(x => x.ResolutionNotes)
            .MaximumLength(2000)
            .When(x => x.ResolutionNotes is not null);
    }
}
