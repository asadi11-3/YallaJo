using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record CategoryCreatedDomainEvent(
    Guid CategoryId,
    string Name,
    string SourceLanguageCode) : DomainEventBase;
