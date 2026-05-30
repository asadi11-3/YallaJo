using FluentValidation;

namespace Messaging.Application.Commands.MarkAllNotificationsRead;

internal sealed class MarkAllNotificationsReadCommandValidator : AbstractValidator<MarkAllNotificationsReadCommand>
{
    public MarkAllNotificationsReadCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
