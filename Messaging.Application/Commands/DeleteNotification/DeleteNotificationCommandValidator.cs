using FluentValidation;

namespace Messaging.Application.Commands.DeleteNotification;

internal sealed class DeleteNotificationCommandValidator : AbstractValidator<DeleteNotificationCommand>
{
    public DeleteNotificationCommandValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
    }
}
