using YallaJo.SharedKernel.Domain.Event;

namespace ContentSeo.Domain.Events;

public sealed record RedirectUpdatedDomainEvent(
    Guid RedirectId,
    string OldUrl,
    string OriginalNewUrl,
    string NewUrl,
    int OriginalStatusCode,
    int StatusCode) : DomainEventBase;
