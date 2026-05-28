using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorProfileDeactivatedIntegrationEvent(
    Guid ProfileId,
    Guid UserId,
    DateTime DeactivatedAtUtc) : IntegrationEventBase;
