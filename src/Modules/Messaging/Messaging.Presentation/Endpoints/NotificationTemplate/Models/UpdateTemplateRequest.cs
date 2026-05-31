namespace Messaging.Presentation.Endpoints.NotificationTemplate.Models;

internal sealed record UpdateTemplateRequest(string Title, string Body, string? HtmlBody);
