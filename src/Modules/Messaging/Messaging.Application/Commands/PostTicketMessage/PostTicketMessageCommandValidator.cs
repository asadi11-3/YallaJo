using FluentValidation;

namespace Messaging.Application.Commands.PostTicketMessage;

internal sealed class PostTicketMessageCommandValidator : AbstractValidator<PostTicketMessageCommand>
{
    public PostTicketMessageCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.AuthorUserId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
