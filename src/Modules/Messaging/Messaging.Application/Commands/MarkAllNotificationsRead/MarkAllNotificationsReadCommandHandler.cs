using MediatR;
using Messaging.Application.Caching;
using Messaging.Domain.Repositories;
using Microsoft.Extensions.Caching.Hybrid;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.MarkAllNotificationsRead;

internal sealed class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    HybridCache cache) : IRequestHandler<MarkAllNotificationsReadCommand, Result>
{
    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        await notificationRepository.MarkAllAsReadByUserAsync(request.UserId, cancellationToken);
        await cache.RemoveByTagAsync(MessagingCacheKeys.NotificationsTag(request.UserId), cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}