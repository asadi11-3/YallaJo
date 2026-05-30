using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when an admin promotes a creator to a higher trust tier.</summary>
public sealed record CreatorTierPromotedDomainEvent(
    Guid CreatorProfileId,
    Guid UserId,
    Guid PromotedByAdminId,
    CreatorTrustTier PreviousTier,
    CreatorTrustTier NewTier) : DomainEventBase;
