using ContentCore.Domain.Enums;

namespace ContentCore.Domain.Entities;

public sealed class EntityImage
{
    private EntityImage() { } // EF Core

    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid AttachmentId { get; private set; }
    public ImageSize ImageSize { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    public Attachment Attachment { get; private set; } = default!;
}
