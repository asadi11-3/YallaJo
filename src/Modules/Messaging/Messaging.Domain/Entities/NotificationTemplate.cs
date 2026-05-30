using Messaging.Domain.Enums;
using Messaging.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class NotificationTemplate : AuditableEntity, IAggregateRoot
{
    private NotificationTemplate() { } // EF Core

    /// <summary>Classification of notification being templated.</summary>
    public NotificationType Type { get; private set; }

    /// <summary>Delivery channel (InApp / Email / Push).</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>BCP-47 language code, e.g. "en" or "ar".</summary>
    public string LanguageCode { get; private set; } = "en";

    /// <summary>Short title / subject line (1-200 chars). Supports {{Placeholder}} tokens.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Plain-text notification body (1-5000 chars). Supports {{Placeholder}} tokens.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>Optional sanitised HTML body for email channel (≤50 000 chars).</summary>
    public string? HtmlBody { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static NotificationTemplate Create(
        NotificationType type,
        NotificationChannel channel,
        string languageCode,
        string title,
        string body,
        string? htmlBody = null)
    {
        var template = new NotificationTemplate
        {
            Type         = type,
            Channel      = channel,
            LanguageCode = languageCode.ToLowerInvariant(),
            Title        = title.Trim(),
            Body         = body.Trim(),
            HtmlBody     = htmlBody?.Trim(),
        };
        template.AddDomainEvent(new NotificationTemplateCreatedDomainEvent(
            template.Id, type, channel, languageCode));
        return template;
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    public void Update(string title, string body, string? htmlBody = null)
    {
        Title    = title.Trim();
        Body     = body.Trim();
        HtmlBody = htmlBody?.Trim();
        AddDomainEvent(new NotificationTemplateUpdatedDomainEvent(Id));
        MarkUpdated();
    }

    public void Delete()
    {
        SoftDelete();
        AddDomainEvent(new NotificationTemplateDeletedDomainEvent(Id));
    }
}
