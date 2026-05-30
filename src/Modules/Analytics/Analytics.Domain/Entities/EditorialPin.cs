using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class EditorialPin : BaseEntity, IAggregateRoot
{
    private EditorialPin() { } // EF Core

    public EntityType EntityKind { get; private set; }
    public Guid EntityId { get; private set; }
    public int Position { get; private set; }
    public SuggestionContext Context { get; private set; }
    public string BadgeText { get; private set; } = "Editor's Choice";
    public bool IsActive { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public static EditorialPin Create(EntityType entityKind, Guid entityId, int position, SuggestionContext context, string? badgeText = null, DateTime? expiresAt = null)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));

        if (position < 1)
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be at least 1.");

        return new EditorialPin
        {
            EntityKind = entityKind,
            EntityId = entityId,
            Position = position,
            Context = context,
            BadgeText = string.IsNullOrWhiteSpace(badgeText) ? "Editor's Choice" : badgeText.Trim(),
            IsActive = true,
            ExpiresAt = expiresAt
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public bool IsExpired(DateTime now) => ExpiresAt.HasValue && now >= ExpiresAt.Value;
}
