using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

[Obsolete("Out of scope for Phase 1. Reserved for Phase 3.")]
public sealed class RecommendationCache : BaseEntity, IAggregateRoot
{
    private RecommendationCache() { } // EF Core

    public Guid UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public decimal Score { get; private set; }
    public string? Reason { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
}


