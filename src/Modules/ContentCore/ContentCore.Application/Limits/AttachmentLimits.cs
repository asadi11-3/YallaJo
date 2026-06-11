using ContentCore.Domain.Enums;

namespace ContentCore.Application.Limits;

/// <summary>
/// Centralized attachment limits per entity type and attachment type.
/// These caps are enforced at upload time to prevent unbounded media accumulation.
/// </summary>
public static class AttachmentLimits
{
    // ── Max attachment count per entity type ─────────────────────────────────

    private static readonly Dictionary<EntityType, int> MaxCountByEntity = new()
    {
        [EntityType.Review]    = 5,
        [EntityType.Blog]      = 20,
        [EntityType.Tour]      = 30,
        [EntityType.Place]     = 30,
        [EntityType.Business]  = 20,
        [EntityType.TourGuide] = 10,
        [EntityType.Creator]   = 3,
    };

    // ── Max file size per attachment type (bytes) ─────────────────────────────
    // Note: validator also enforces per-type sizes.
    // These are the canonical limits referenced by both validator and handler.

    public static readonly long MaxImageBytes    =  10L * 1024 * 1024;  // 10 MB
    public static readonly long MaxVideoBytes    = 500L * 1024 * 1024;  // 500 MB
    public static readonly long MaxDocumentBytes =  25L * 1024 * 1024;  // 25 MB
    public static readonly long MaxAudioBytes    =  50L * 1024 * 1024;  // 50 MB

    /// <summary>
    /// Returns the maximum number of attachments allowed for the given entity type.
    /// Returns <c>int.MaxValue</c> if no limit is configured.
    /// </summary>
    public static int GetMaxCount(EntityType entityType)
        => MaxCountByEntity.TryGetValue(entityType, out var max) ? max : int.MaxValue;

    /// <summary>
    /// Returns the maximum file size in bytes for the given attachment type.
    /// Returns <see cref="long.MaxValue"/> if no limit is configured.
    /// </summary>
    public static long GetMaxFileSize(AttachmentType attachmentType) =>
        attachmentType switch
        {
            AttachmentType.Image    => MaxImageBytes,
            AttachmentType.Video    => MaxVideoBytes,
            AttachmentType.Document => MaxDocumentBytes,
            AttachmentType.Audio    => MaxAudioBytes,
            _                       => long.MaxValue,
        };

    /// <summary>
    /// Returns <c>true</c> if <paramref name="currentCount"/> is at or above the cap.
    /// </summary>
    public static bool IsAtLimit(EntityType entityType, int currentCount)
        => currentCount >= GetMaxCount(entityType);
}
