using MediatR;
using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyNotifications;

internal sealed class GetMyNotificationsQueryHandler(
    INotificationRepository notificationRepository) : IRequestHandler<GetMyNotificationsQuery, Result<NotificationPageDto>>
{
    public async Task<Result<NotificationPageDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var (items, nextCursor) = await notificationRepository.GetByUserPagedAsync(
            request.UserId, request.Type, request.IsRead, request.From, request.To, request.AfterCursor, pageSize, cancellationToken);

        var dtos = items.Select(n => new NotificationDto(
            n.Id, n.UserId, n.Type.ToString(), n.Channel.ToString(), n.Priority.ToString(),
            n.Title, n.Body, n.Data, n.IsRead, n.ReadAt, n.SentAt, n.EntityType, n.EntityId, n.CreatedAt))
            .ToList();

        return Result.Success(new NotificationPageDto(dtos, nextCursor));
    }
}
