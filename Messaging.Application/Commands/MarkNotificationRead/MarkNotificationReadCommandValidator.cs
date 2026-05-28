using FluentValidation;

namespace Messaging.Application.Commands.MarkNotificationRead;

internal sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
    }
}
