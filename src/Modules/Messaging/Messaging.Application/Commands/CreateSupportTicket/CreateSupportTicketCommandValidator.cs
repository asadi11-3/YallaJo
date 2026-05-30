using FluentValidation;

namespace Messaging.Application.Commands.CreateSupportTicket;

internal sealed class CreateSupportTicketCommandValidator : AbstractValidator<CreateSupportTicketCommand>
{
    public CreateSupportTicketCommandValidator()
    {
        RuleFor(x => x.CreatedByUserId).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
