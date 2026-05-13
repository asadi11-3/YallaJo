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

    public static string Category(Guid id, bool withTranslations, bool includeInactive = false) =>
        $"cc:cat:{id}:{withTranslations}:{includeInactive}";

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

    // ── Languages (single) ──────────────────────────────────────────────────
    public static string LanguageById(Guid id) => $"cc:lang:{id}";

    // ── Specializations ─────────────────────────────────────────────────────
    public static string Specializations(bool activeOnly) =>
        $"cc:specs:{activeOnly}";

    public static string SpecializationById(Guid id) => $"cc:spec:{id}";

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

    public static string EntityTranslations(
        string entityType,
        Guid entityId,
        string? languageCode,
        ContentCore.Domain.Enums.TranslationStatus? status) =>
        $"cc:trans:{entityType}:{entityId}:{languageCode ?? "all"}:{status?.ToString() ?? "all"}";

    // ── Cache Tags (for HybridCache.RemoveByTagAsync invalidation) ─────────
    // NOTE: tag strings are intentionally distinct from key strings.
    // Tags here MUST remain byte-for-byte identical to the historical literals
    // used by query Tags properties; do not change values without a migration plan.

    public const string CategoriesTag = "categories";
    public const string TagsTag = "tags";
    public const string LanguagesTag = "languages";
    public const string SpecializationsTag = "specializations";
    public const string AttachmentsTag = "attachments";
    public const string TranslationsTag = "translations";

    public static string CategoryTag(Guid id) => $"category:{id}";

    public static string TagTag(Guid id) => $"tag:{id}";

    public static string LanguageTag(Guid id) => $"language:{id}";

    public static string SpecializationTag(Guid id) => $"specialization:{id}";

    public static string AttachmentTag(Guid id) => $"attachment:{id}";

    public static string EntityAttachmentsTag(string entityType, Guid entityId) =>
        $"attachments:{entityType}:{entityId}";

    public static string EntityCategoriesTag(string entityType, Guid entityId) =>
        $"entity-categories:{entityType}:{entityId}";

    public static string EntityTagsTag(string entityType, Guid entityId) =>
        $"entity-tags:{entityType}:{entityId}";

    public static string EntityTranslationsTag(string entityType, Guid entityId) =>
        $"translations:{entityType}:{entityId}";
}
