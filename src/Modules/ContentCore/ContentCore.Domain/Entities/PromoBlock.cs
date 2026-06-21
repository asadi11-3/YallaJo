using ContentCore.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

/// <summary>
/// A single promotional/ad placement rendered on a page slot (identified by
/// <see cref="PlacementKey"/>, e.g. <c>MyProfile.RightRail.Top</c>).
///
/// Promo blocks are pre-seeded per placement key and edited in place by
/// privileged users; they are not created/deleted through the inline editor.
/// </summary>
public sealed class PromoBlock : AuditableEntity, IAggregateRoot
{
    private PromoBlock() { } // EF Core

    /// <summary>Stable slot identifier (unique). e.g. <c>MyProfile.RightRail.Top</c>.</summary>
    public string PlacementKey { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public Guid? AttachmentId { get; private set; }
    public string? ButtonText { get; private set; }
    public string? ButtonUrl { get; private set; }
    public string? BadgeText { get; private set; }
    public string? IconName { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }
    public DateTimeOffset? StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }

    // ── Factory Method (the ONLY way to create) ──
    public static PromoBlock Create(
        string placementKey,
        string title,
        string? description = null,
        string? buttonText = null,
        string? buttonUrl = null,
        string? badgeText = null,
        string? iconName = null,
        string? imageUrl = null,
        bool isActive = true,
        int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(placementKey))
            throw new ArgumentException("Promo block placement key is required.", nameof(placementKey));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Promo block title is required.", nameof(title));

        return new PromoBlock
        {
            PlacementKey = placementKey.Trim(),
            Title = title.Trim(),
            Description = Normalize(description),
            ButtonText = Normalize(buttonText),
            ButtonUrl = Normalize(buttonUrl),
            BadgeText = Normalize(badgeText),
            IconName = Normalize(iconName),
            ImageUrl = Normalize(imageUrl),
            IsActive = isActive,
            SortOrder = sortOrder
        };
    }

    // ── Business Methods ──

    /// <summary>Updates editable content fields (everything except the image binary).</summary>
    public void UpdateContent(
        string title,
        string? description,
        string? buttonText,
        string? buttonUrl,
        string? badgeText,
        string? iconName,
        bool isActive,
        int sortOrder,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Promo block title is required.", nameof(title));

        if (startsAt.HasValue && endsAt.HasValue && endsAt.Value < startsAt.Value)
            throw new ArgumentException("Promo block end date cannot be before its start date.", nameof(endsAt));

        Title = title.Trim();
        Description = Normalize(description);
        ButtonText = Normalize(buttonText);
        ButtonUrl = Normalize(buttonUrl);
        BadgeText = Normalize(badgeText);
        IconName = Normalize(iconName);
        IsActive = isActive;
        SortOrder = sortOrder;
        StartsAt = startsAt;
        EndsAt = endsAt;
        MarkUpdated();

        AddDomainEvent(new PromoBlockUpdatedDomainEvent(Id, PlacementKey));
    }

    /// <summary>Sets the promo image after the binary has been stored via the attachment pipeline.</summary>
    public void SetImage(string? imageUrl, Guid? attachmentId)
    {
        ImageUrl = Normalize(imageUrl);
        AttachmentId = attachmentId;
        MarkUpdated();

        AddDomainEvent(new PromoBlockImageChangedDomainEvent(Id, PlacementKey, ImageUrl, AttachmentId));
    }

    /// <summary>Clears the promo image, reverting to the placeholder/empty state.</summary>
    public void ClearImage()
    {
        ImageUrl = null;
        AttachmentId = null;
        MarkUpdated();

        AddDomainEvent(new PromoBlockImageChangedDomainEvent(Id, PlacementKey, null, null));
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        MarkUpdated();
    }

    /// <summary>
    /// True when the block should be visible to the public right now:
    /// active, not soft-deleted, and within its optional scheduling window.
    /// </summary>
    public bool IsVisibleAt(DateTimeOffset now) =>
        IsActive
        && !IsDeleted
        && (StartsAt is null || now >= StartsAt.Value)
        && (EndsAt is null || now <= EndsAt.Value);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
