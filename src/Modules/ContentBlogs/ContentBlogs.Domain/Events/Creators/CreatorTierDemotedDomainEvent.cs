using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Event;

namespace ContentBlogs.Domain.Events.Creators;

/// <summary>Raised when a creator is demoted to a lower trust tier (auto or manual).</summary>
public sealed record CreatorTierDemotedDomainEvent(
    Guid CreatorProfileId,
    Guid UserId,
    CreatorTrustTier PreviousTier,
    CreatorTrustTier NewTier,
    string Reason) : DomainEventBase;
