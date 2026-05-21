using Messaging.Application.Interfaces;
using Messaging.Domain.Enums;

namespace Messaging.Infrastructure.Services;

internal sealed class NoopNotificationTemplateRenderer : INotificationTemplateRenderer
{
    public Task<RenderedTemplate> RenderAsync(
        NotificationType type, NotificationChannel channel, string languageCode,
        IReadOnlyDictionary<string, string> placeholders, CancellationToken ct = default)
        => Task.FromResult(new RenderedTemplate(
            Title: $"[{type}]",
            Body: "[Template not found]",
            HtmlBody: null));
}
