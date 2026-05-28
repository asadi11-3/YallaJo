using FluentValidation;

namespace Messaging.Application.Commands.AssignSupportTicket;

internal sealed class AssignSupportTicketCommandValidator : AbstractValidator<AssignSupportTicketCommand>
{
    public AssignSupportTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.AdminUserId).NotEmpty();
        RuleFor(x => x.AssignedByUserId).NotEmpty();
    }
}
