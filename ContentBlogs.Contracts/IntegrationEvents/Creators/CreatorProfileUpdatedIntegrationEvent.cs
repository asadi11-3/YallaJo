using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorProfileUpdatedIntegrationEvent(
    Guid ProfileId,
    Guid UserId,
    string? OldSlug,
    string NewSlug,
    DateTime UpdatedAtUtc) : IntegrationEventBase;
