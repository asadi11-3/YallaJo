using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class RecommendationCache : BaseEntity, IAggregateRoot
{
    private RecommendationCache() { } // EF Core

    public Guid? UserId { get; private set; }
    public Guid BatchId { get; private set; }
    public EntityType EntityKind { get; private set; }
    public Guid EntityId { get; private set; }
    public decimal Score { get; private set; }
    public int Position { get; private set; }
    public string? SignalsJson { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public static RecommendationCache Create(Guid? userId, Guid batchId, EntityType entityKind, Guid entityId, decimal score, int position, string? signalsJson, DateTime generatedAt, DateTime expiresAt)
    {
        if (batchId == Guid.Empty)
            throw new ArgumentException("Batch id cannot be empty.", nameof(batchId));

        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));

        if (position <= 0)
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be greater than zero.");

        if (expiresAt <= generatedAt)
            throw new ArgumentException("Expiration must be after generation time.", nameof(expiresAt));

        return new RecommendationCache
        {
            UserId = userId,
            BatchId = batchId,
            EntityKind = entityKind,
            EntityId = entityId,
            Score = score,
            Position = position,
            SignalsJson = string.IsNullOrWhiteSpace(signalsJson) ? null : signalsJson.Trim(),
            GeneratedAt = generatedAt,
            ExpiresAt = expiresAt
        };
    }
}


