using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record TagUpdatedDomainEvent(
    Guid TagId,
    string Name,
    string SourceLanguageCode) : DomainEventBase;
