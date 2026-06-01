using FluentValidation;

namespace Messaging.Application.Commands.CloseSupportTicket;

internal sealed class CloseSupportTicketCommandValidator : AbstractValidator<CloseSupportTicketCommand>
{
    public CloseSupportTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();

        RuleFor(x => x.RowVersion)
            .NotNull()
            .Must(rv => rv is { Length: > 0 })
            .WithMessage("RowVersion is required for optimistic concurrency.");
    }
}
