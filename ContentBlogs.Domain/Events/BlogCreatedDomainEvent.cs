using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogCreatedDomainEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid SourceLanguageId,
    Guid? PlaceId,
    DateTime CreatedAtUtc) : DomainEventBase;
