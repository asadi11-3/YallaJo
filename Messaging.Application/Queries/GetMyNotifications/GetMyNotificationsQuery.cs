using Messaging.Application.Queries.Dtos;
using Messaging.Domain.Enums;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(
    Guid UserId,
    NotificationType? Type = null,
    bool? IsRead = null,
    DateTime? From = null,
    DateTime? To = null,
    Guid? AfterCursor = null,
    int PageSize = 20) : IRequest<Result<NotificationPageDto>>;
