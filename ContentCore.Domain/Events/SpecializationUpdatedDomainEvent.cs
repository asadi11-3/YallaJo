using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record SpecializationUpdatedDomainEvent(
    Guid SpecializationId,
    string Name,
    string? Description,
    string SourceLanguageCode) : DomainEventBase;
