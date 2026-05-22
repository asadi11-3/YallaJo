using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorPostPublishedIntegrationEvent(
    Guid PostId,
    Guid CreatorProfileId,
    string Title,
    bool IsPreModerated,
    DateTime PublishedAtUtc) : IntegrationEventBase;
