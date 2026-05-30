using Analytics.Domain.Enums;
using Analytics.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class SuggestionBatch : AuditableEntity, IAggregateRoot
{
    private SuggestionBatch() { } // EF Core

    public EntityType SourceKind { get; private set; }
    public Guid SourceId { get; private set; }
    public SuggestionContext Context { get; private set; }
    public string AlgorithmVersion { get; private set; } = string.Empty;
    public DateTime ComputedAt { get; private set; }
    public bool IsStale { get; private set; }
    public int ItemCount { get; private set; }

    public static SuggestionBatch Create(EntityType sourceKind, Guid sourceId, SuggestionContext context, string algorithmVersion, int itemCount, DateTime computedAt)
    {
        if (sourceId == Guid.Empty)
            throw new ArgumentException("Source id cannot be empty.", nameof(sourceId));

        if (string.IsNullOrWhiteSpace(algorithmVersion))
            throw new ArgumentException("Algorithm version is required.", nameof(algorithmVersion));

        if (itemCount < 0)
            throw new ArgumentOutOfRangeException(nameof(itemCount), "Item count cannot be negative.");

        var batch = new SuggestionBatch
        {
            SourceKind = sourceKind,
            SourceId = sourceId,
            Context = context,
            AlgorithmVersion = algorithmVersion.Trim(),
            ComputedAt = computedAt,
            IsStale = false,
            ItemCount = itemCount
        };

        batch.AddDomainEvent(new SuggestionBatchCreatedDomainEvent(batch.Id, (int)sourceKind, sourceId, (int)context, batch.AlgorithmVersion, itemCount, computedAt));
        return batch;
    }

    public void MarkStale()
    {
        if (IsStale) return;

        IsStale = true;
        MarkUpdated();
        AddDomainEvent(new SuggestionBatchStaleFlaggedDomainEvent(Id, (int)SourceKind, SourceId, (int)Context, UpdatedAt ?? DateTime.UtcNow));
    }

    public void MarkRefreshed(int itemCount, string algorithmVersion)
    {
        if (itemCount < 0)
            throw new ArgumentOutOfRangeException(nameof(itemCount), "Item count cannot be negative.");

        if (string.IsNullOrWhiteSpace(algorithmVersion))
            throw new ArgumentException("Algorithm version is required.", nameof(algorithmVersion));

        ItemCount = itemCount;
        AlgorithmVersion = algorithmVersion.Trim();
        ComputedAt = DateTime.UtcNow;
        IsStale = false;
        MarkUpdated();
        AddDomainEvent(new SuggestionBatchRefreshedDomainEvent(Id, (int)SourceKind, SourceId, (int)Context, AlgorithmVersion, itemCount, ComputedAt));
    }
}
