using Messaging.Domain.Enums;

namespace Messaging.Presentation.Endpoints.Notification.Models;

internal sealed record GetNotificationsRequest(
    NotificationType? Type,
    bool? IsRead,
    DateTime? From,
    DateTime? To,
    Guid? Cursor,
    int? PageSize);
