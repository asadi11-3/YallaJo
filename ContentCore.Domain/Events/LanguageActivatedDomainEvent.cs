using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record LanguageActivatedDomainEvent(
    Guid LanguageId,
    string LanguageCode) : DomainEventBase;
