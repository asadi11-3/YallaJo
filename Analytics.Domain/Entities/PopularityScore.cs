using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class PopularityScore : AuditableEntity
{
    private PopularityScore() { } // EF Core

    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public decimal TrendingScore { get; private set; }
    public int ViewCount { get; private set; }
    public int BookmarkCount { get; private set; }
    public int ShareCount { get; private set; }
    public int BookingCount { get; private set; }
    public decimal ReviewScore { get; private set; }
    public DateTime LastCalculatedAt { get; private set; }
}
