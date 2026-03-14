namespace ContentCore.Application.Caching;

/// <summary>
/// Centralized cache key factory for all ContentCore cached queries.
/// Every key is deterministic — same inputs always produce the same key.
/// Command handlers use these to invalidate cache entries after writes.
/// </summary>
public static class ContentCoreCacheKeys
{
    // ── Languages ──────────────────────────────────────────────────────────
    public static string Languages(bool activeOnly) =>
        $"cc:langs:{activeOnly}";

    // ── Categories ─────────────────────────────────────────────────────────
    public static string CategoryList(bool activeOnly, Guid? parentId, bool withTranslations) =>
        $"cc:cats:{activeOnly}:{parentId?.ToString() ?? "root"}:{withTranslations}";

    public static string Category(Guid id, bool withTranslations) =>
        $"cc:cat:{id}:{withTranslations}";

    /// <summary>
    /// Returns the known "root-level" category list cache key permutations.
    /// Command handlers remove these on any category write to keep the main tree fresh.
    /// Parameterized variants (specific parentId) will expire via their TTL.
    /// </summary>
    public static IEnumerable<string> CommonCategoryListKeys()
    {
        yield return CategoryList(false, null, false);
        yield return CategoryList(true,  null, false);
        yield return CategoryList(false, null, true);
        yield return CategoryList(true,  null, true);
    }

    // ── Tags ────────────────────────────────────────────────────────────────
    public static string Tags(bool activeOnly) =>
        $"cc:tags:{activeOnly}";

    public static string Tag(Guid id) =>
        $"cc:tag:{id}";

    // ── Specializations ─────────────────────────────────────────────────────
    public static string Specializations(bool activeOnly) =>
        $"cc:specs:{activeOnly}";

    // ── Attachments ─────────────────────────────────────────────────────────
    public static string Attachment(Guid id) =>
        $"cc:att:{id}";

    public static string EntityAttachments(string entityType, Guid entityId) =>
        $"cc:atts:{entityType}:{entityId}";

    // ── Entity-Category / Entity-Tag ────────────────────────────────────────
    public static string EntityCategories(string entityType, Guid entityId) =>
        $"cc:entcat:{entityType}:{entityId}";

    public static string EntityTags(string entityType, Guid entityId) =>
        $"cc:enttag:{entityType}:{entityId}";

    // ── Translations ────────────────────────────────────────────────────────
    public static string EntityTranslations(string entityType, Guid entityId) =>
        $"cc:trans:{entityType}:{entityId}";
}
