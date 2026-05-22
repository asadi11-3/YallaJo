using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorPostRemovedIntegrationEvent(
    Guid PostId,
    Guid CreatorProfileId,
    DateTime RemovedAtUtc) : IntegrationEventBase;
