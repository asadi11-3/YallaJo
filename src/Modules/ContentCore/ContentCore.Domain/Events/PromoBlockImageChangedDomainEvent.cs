using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

public sealed record PromoBlockImageChangedDomainEvent(
    Guid PromoBlockId,
    string PlacementKey,
    string? ImageUrl,
    Guid? AttachmentId) : DomainEventBase;
