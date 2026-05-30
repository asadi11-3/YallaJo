using FluentValidation;

namespace Messaging.Application.Commands.BatchDeleteNotifications;

internal sealed class BatchDeleteNotificationsCommandValidator : AbstractValidator<BatchDeleteNotificationsCommand>
{
    public BatchDeleteNotificationsCommandValidator()
    {
        RuleFor(x => x.NotificationIds).NotEmpty();
        RuleFor(x => x.CallerUserId).NotEmpty();
    }
}
