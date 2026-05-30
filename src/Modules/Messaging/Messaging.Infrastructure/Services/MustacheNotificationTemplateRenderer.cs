using Messaging.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Messaging.Domain.Enums;
using Messaging.Domain.Repositories;
using Stubble.Core.Builders;
using Stubble.Core;

namespace Messaging.Infrastructure.Services;

/// <summary>Mustache template renderer backed by NotificationTemplate table. M-R4 fallback chain.</summary>
internal sealed class MustacheNotificationTemplateRenderer(
    INotificationTemplateRepository templateRepository,
    ILogger<MustacheNotificationTemplateRenderer> logger)
    : INotificationTemplateRenderer
{
    private static readonly StubbleVisitorRenderer _stubble = new StubbleBuilder().Build();

    public async Task<RenderedTemplate> RenderAsync(
        NotificationType type,
        NotificationChannel channel,
        string languageCode,
        IReadOnlyDictionary<string, string> placeholders,
        CancellationToken ct = default)
    {
        // M-R4 fallback chain: (type,channel,lang) → (type,channel,"en") → (type,InApp,"en") → inline
        var template =
            await templateRepository.GetByKeyAndLanguageAsync(type, channel, languageCode, ct)
            ?? await templateRepository.GetByKeyAndLanguageAsync(type, channel, "en", ct)
            ?? await templateRepository.GetByKeyAndLanguageAsync(type, NotificationChannel.InApp, "en", ct);

        if (template is null)
        {
            logger.LogWarning("No template found for {Type}/{Channel}/{Lang} — using inline fallback", type, channel, languageCode);
            return new RenderedTemplate(Title: $"{type}", Body: $"[{type}]");
        }

        var values = placeholders.ToDictionary(k => k.Key, v => (object)v.Value);
        var title = await _stubble.RenderAsync(template.Title, values);
        var body = await _stubble.RenderAsync(template.Body, values);
        string? htmlBody = template.HtmlBody is not null
            ? await _stubble.RenderAsync(template.HtmlBody, values)
            : null;

        return new RenderedTemplate(title, body, htmlBody);
    }
}
