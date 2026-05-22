using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorPostRejectedIntegrationEvent(
    Guid PostId,
    Guid CreatorProfileId,
    Guid RejectedByAdminId,
    string Reason,
    DateTime RejectedAtUtc) : IntegrationEventBase;
