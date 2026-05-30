using MediatR;
using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetNotificationById;

internal sealed class GetNotificationByIdQueryHandler(
    INotificationRepository notificationRepository) : IRequestHandler<GetNotificationByIdQuery, Result<NotificationDto>>
{
    public async Task<Result<NotificationDto>> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var n = await notificationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (n is null)
            return Result.Failure<NotificationDto>(new Error("Notification.NotFound", "Notification not found."), Outcome.NotFound);

        if (n.UserId != request.CallerUserId)
            return Result.Failure<NotificationDto>(new Error("Notification.OwnerMismatch", "You do not own this notification."), Outcome.Forbidden);

        return Result.Success(new NotificationDto(
            n.Id, n.UserId, n.Type.ToString(), n.Channel.ToString(), n.Priority.ToString(),
            n.Title, n.Body, n.Data, n.IsRead, n.ReadAt, n.SentAt, n.EntityType, n.EntityId, n.CreatedAt));
    }
}
