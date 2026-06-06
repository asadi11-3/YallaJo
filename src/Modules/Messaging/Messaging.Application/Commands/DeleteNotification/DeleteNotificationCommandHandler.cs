using MediatR;
using Messaging.Application.Caching;
using Messaging.Application.Interfaces;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.DeleteNotification;

internal sealed class DeleteNotificationCommandHandler(
    INotificationRepository notificationRepository,
    IMessagingUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<DeleteNotificationCommand, Result>
{
    public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdForUpdateAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            return Result.Failure(new Error("Notification.NotFound", "Notification not found."), Outcome.NotFound);

        if (notification.UserId != request.CallerUserId)
            return Result.Failure(new Error("Notification.OwnerMismatch", "You do not own this notification."), Outcome.Forbidden);

        // M-R1: Critical types NEVER deleted
        if (notification.Type.IsCritical())
            return Result.Failure(new Error("Notification.CannotDeleteCritical", "Critical notifications cannot be deleted."), Outcome.UnprocessableEntity);

        notification.SoftDelete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.RemoveByTagAsync(MessagingCacheKeys.NotificationsTag(notification.UserId), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
