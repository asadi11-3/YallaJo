using FluentValidation;

namespace Messaging.Application.Commands.CloseSupportTicket;

internal sealed class CloseSupportTicketCommandValidator : AbstractValidator<CloseSupportTicketCommand>
{
    public CloseSupportTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
    }
}
