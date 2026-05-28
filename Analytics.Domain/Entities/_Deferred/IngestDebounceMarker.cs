using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class IngestDebounceMarker : BaseEntity
{
    private IngestDebounceMarker() { }
    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public DateTime LastFlagged { get; private set; }
    public static IngestDebounceMarker Create(EntityType entityType, Guid entityId, DateTime now)
        => new() { EntityType = entityType, EntityId = entityId, LastFlagged = now };
    public void Refresh(DateTime now) => LastFlagged = now;
}
