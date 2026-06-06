using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.MarkNotificationRead;

internal sealed class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache,
    TimeProvider timeProvider) : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdForUpdateAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            return Result.Failure(new Error("Notification.NotFound", "Notification not found."), Outcome.NotFound);

        if (notification.UserId != request.CallerUserId)
            return Result.Failure(new Error("Notification.OwnerMismatch", "You do not own this notification."), Outcome.Forbidden);

        notification.MarkRead(timeProvider); // idempotent
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(MessagingCacheKeys.NotificationsTag(notification.UserId), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
