using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.BatchDeleteNotifications;

internal sealed class BatchDeleteNotificationsCommandHandler(
    INotificationRepository notificationRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<BatchDeleteNotificationsCommand, Result>
{
    public async Task<Result> Handle(BatchDeleteNotificationsCommand request, CancellationToken cancellationToken)
    {
        if (request.NotificationIds.Count is 0)
            return Result.Failure(new Error("Notification.EmptyBatch", "At least one notification id is required."), Outcome.UnprocessableEntity);

        if (request.NotificationIds.Count > 100)
            return Result.Failure(new Error("Notification.BatchTooLarge", "Batch delete supports up to 100 notifications."), Outcome.UnprocessableEntity);

        foreach (var id in request.NotificationIds.Distinct())
        {
            var notification = await notificationRepository.GetByIdAsync(id, cancellationToken);
            if (notification is null)
                return Result.Failure(new Error("Notification.NotFound", "Notification not found."), Outcome.NotFound);

            if (notification.UserId != request.CallerUserId)
                return Result.Failure(new Error("Notification.OwnerMismatch", "You do not own this notification."), Outcome.Forbidden);

            if (notification.Type.IsCritical())
                return Result.Failure(new Error("Notification.CannotDeleteCritical", "Critical notifications cannot be deleted."), Outcome.UnprocessableEntity);

            notification.SoftDelete();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(MessagingCacheKeys.NotificationsTag(request.CallerUserId), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
