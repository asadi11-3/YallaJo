using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorProfileSuspendedIntegrationEvent(
    Guid ProfileId,
    Guid UserId,
    Guid SuspendedByAdminId,
    string Reason,
    DateTime SuspendedAtUtc) : IntegrationEventBase;
