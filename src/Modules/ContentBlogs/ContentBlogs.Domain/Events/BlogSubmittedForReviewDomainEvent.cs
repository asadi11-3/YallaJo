using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events;

public sealed record BlogSubmittedForReviewDomainEvent(
    Guid BlogId,
    string Slug,
    Guid CreatorProfileId,
    DateTime SubmittedAtUtc) : DomainEventBase;
