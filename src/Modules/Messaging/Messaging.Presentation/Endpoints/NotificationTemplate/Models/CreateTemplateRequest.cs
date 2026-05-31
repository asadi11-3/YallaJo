using Messaging.Domain.Enums;

namespace Messaging.Presentation.Endpoints.NotificationTemplate.Models;

internal sealed record CreateTemplateRequest(
    NotificationType Type,
    NotificationChannel Channel,
    string LanguageCode,
    string Title,
    string Body,
    string? HtmlBody);
