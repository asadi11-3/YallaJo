using ContentCore.Domain.Enums;
using ContentCore.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Attachment : BaseEntity, IAggregateRoot
{
    private Attachment() { } // EF Core

    public EntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public AttachmentType Type { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? ThumbnailUrl { get; private set; }
    public string? OriginalFileName { get; private set; }
    public string? MimeType { get; private set; }
    public long? FileSize { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public int? DurationSeconds { get; private set; }
    public int SortOrder { get; private set; }
    public byte[]? Iv { get; private set; }
    public byte[]? Hmac { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid UploadedByUserId { get; private set; }

    // ── Factory Method (the ONLY way to create) ──
    public static Attachment Create(
        EntityType entityType,
        Guid entityId,
        AttachmentType type,
        string url,
        Guid uploadedByUserId,
        string? originalFileName = null,
        string? mimeType = null,
        long? fileSize = null,
        int sortOrder = 0)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity ID is required.", nameof(entityId));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Attachment URL is required.", nameof(url));

        if (uploadedByUserId == Guid.Empty)
            throw new ArgumentException("Uploader user ID is required.", nameof(uploadedByUserId));

        var attachment = new Attachment
        {
            EntityType = entityType,
            EntityId = entityId,
            Type = type,
            Url = url.Trim(),
            OriginalFileName = originalFileName?.Trim(),
            MimeType = mimeType?.Trim(),
            FileSize = fileSize,
            SortOrder = sortOrder,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = uploadedByUserId
        };

        attachment.AddDomainEvent(new AttachmentUploadedDomainEvent(
            attachment.Id,
            entityType,
            entityId,
            type,
            attachment.Url));

        return attachment;
    }

    // ── Business Methods ──

    public void SetThumbnailUrl(string? thumbnailUrl)
    {
        ThumbnailUrl = thumbnailUrl?.Trim();
    }

    public void SetDimensions(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentException("Width must be positive.", nameof(width));

        if (height <= 0)
            throw new ArgumentException("Height must be positive.", nameof(height));

        Width = width;
        Height = height;
    }

    public void SetDuration(int durationSeconds)
    {
        if (durationSeconds <= 0)
            throw new ArgumentException("Duration must be positive.", nameof(durationSeconds));

        DurationSeconds = durationSeconds;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public void SetEncryption(byte[] iv, byte[] hmac)
    {
        Iv = iv ?? throw new ArgumentNullException(nameof(iv));
        Hmac = hmac ?? throw new ArgumentNullException(nameof(hmac));
    }

    public void MarkForDeletion()
    {
        AddDomainEvent(new AttachmentDeletedDomainEvent(
            Id,
            EntityType,
            EntityId,
            Type,
            Url));
    }
}
