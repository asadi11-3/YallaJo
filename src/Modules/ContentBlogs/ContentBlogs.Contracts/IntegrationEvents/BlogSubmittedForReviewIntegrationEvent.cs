using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents;

public sealed record BlogSubmittedForReviewIntegrationEvent(
    Guid BlogId,
    string Slug,
    string Title,
    Guid AuthorId,
    Guid? AuthoredByCreatorProfileId,
    DateTime SubmittedAt) : IntegrationEventBase;
