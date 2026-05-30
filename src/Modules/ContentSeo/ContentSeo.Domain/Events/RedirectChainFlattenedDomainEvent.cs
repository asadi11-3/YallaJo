using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

/// <summary>
/// Raised when a redirect-chain compaction job rewrites N hops into a single
/// direct redirect (A → B → C becomes A → C). Outbox handler maps to
/// <c>content-seo.redirect.chain-flattened.v1</c>.
/// </summary>
public sealed record RedirectChainFlattenedDomainEvent(
    Guid RedirectId,
    string OldUrl,
    string OriginalNewUrl,
    string FlattenedNewUrl,
    int HopsCollapsed) : DomainEventBase;
