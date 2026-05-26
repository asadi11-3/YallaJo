using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorProfileActivatedIntegrationEvent(
    Guid ProfileId,
    Guid UserId,
    string DisplayName,
    string Slug,
    DateTime ActivatedAtUtc) : IntegrationEventBase;
