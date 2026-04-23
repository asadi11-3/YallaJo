using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record SpecializationCreatedDomainEvent(
    Guid SpecializationId,
    string Name,
    string? Description,
    string SourceLanguageCode) : DomainEventBase;
