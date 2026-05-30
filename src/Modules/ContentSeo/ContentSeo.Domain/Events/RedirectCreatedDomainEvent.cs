using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when a new <see cref="Entities.Redirect"/> is created.
/// Outbox handler maps to <c>content-seo.redirect.created.v1</c>.
/// </summary>
public sealed record RedirectCreatedDomainEvent(
    Guid RedirectId,
    string OldUrl,
    string NewUrl,
    int StatusCode) : DomainEventBase;
