using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class RecommendationCache : BaseEntity
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
