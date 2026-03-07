using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record CategoryUpdatedDomainEvent(
    Guid CategoryId,
    string Name,
    string SourceLanguageCode) : DomainEventBase;
