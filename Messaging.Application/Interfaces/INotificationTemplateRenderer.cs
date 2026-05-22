using Messaging.Domain.Enums;

namespace Messaging.Application.Interfaces;

/// <summary>Renders a Mustache notification template given placeholders.</summary>
public interface INotificationTemplateRenderer
{
    /// <summary>
    /// Renders template for the given (type, channel, language) combo.
    /// Falls back: (type,channel,lang) → (type,channel,"en") → (type,InApp,"en") → inline last-resort.
    /// </summary>
    Task<RenderedTemplate> RenderAsync(
        NotificationType type,
        NotificationChannel channel,
        string languageCode,
        IReadOnlyDictionary<string, string> placeholders,
        CancellationToken ct = default);
}

/// <summary>Result of template rendering.</summary>
public sealed record RenderedTemplate(string Title, string Body, string? HtmlBody = null);
