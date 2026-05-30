using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorProfileReinstatedIntegrationEvent(
    Guid ProfileId,
    Guid UserId,
    Guid ReinstatedByAdminId,
    DateTime ReinstatedAtUtc) : IntegrationEventBase;
