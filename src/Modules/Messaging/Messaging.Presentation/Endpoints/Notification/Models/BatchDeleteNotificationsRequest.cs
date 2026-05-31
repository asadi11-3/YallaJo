namespace Messaging.Presentation.Endpoints.Notification.Models;

internal sealed record BatchDeleteNotificationsRequest(IReadOnlyList<Guid> Ids);
