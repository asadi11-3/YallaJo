using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class EntityPopularitySnapshot : BaseEntity<long>
{
    private EntityPopularitySnapshot() { }
    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public decimal Score { get; private set; }
    public DateTime TakenAt { get; private set; }
    public static EntityPopularitySnapshot Take(EntityType entityType, Guid entityId, decimal score, DateTime takenAt)
        => new() { EntityType = entityType, EntityId = entityId, Score = score, TakenAt = takenAt };
}
