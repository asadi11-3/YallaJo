using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Contracts.IntegrationEvents.Creators;

public sealed record CreatorPostFeaturedIntegrationEvent(
    Guid PostId,
    Guid CreatorProfileId,
    Guid FeaturedByAdminId,
    DateTime? FeaturedUntilUtc,
    DateTime FeaturedAtUtc) : IntegrationEventBase;
