using YallaJo.SharedKernel.Domain.Event;

namespace ContentTours.Domain.Events;

public sealed record TourCreatedDomainEvent(
    Guid TourId,
    string Name,
    string? Description,
    string? ShortDescription,
    string? MetaTitle,
    string? MetaDescription,
    string Slug,
    Guid CreatedByUserId,
    Guid? PlaceId) : DomainEventBase;
