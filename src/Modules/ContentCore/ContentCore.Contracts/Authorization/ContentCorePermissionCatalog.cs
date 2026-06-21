using YallaJo.SharedKernel.Application.Authorization;

namespace ContentCore.Contracts.Authorization;

/// <summary>
/// Permission catalog for the ContentCore bounded context.
/// Registered in ContentCore.Infrastructure DI — discovered automatically by PermissionSeeder.
/// </summary>
public sealed class ContentCorePermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "ContentCore";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── Category ────────────────────────────────────────────────────────
        new(ContentCoreFeatures.Category, AppAction.Read,   PermissionGroup.ContentManagement, "View categories"),
        new(ContentCoreFeatures.Category, AppAction.Create, PermissionGroup.ContentManagement, "Create a category"),
        new(ContentCoreFeatures.Category, AppAction.Update, PermissionGroup.ContentManagement, "Update a category"),
        new(ContentCoreFeatures.Category, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a category"),

        // ── CategoryTranslation ──────────────────────────────────────────────
        new(ContentCoreFeatures.CategoryTranslation, AppAction.Read,   PermissionGroup.ContentManagement, "View category translations"),
        new(ContentCoreFeatures.CategoryTranslation, AppAction.Create, PermissionGroup.ContentManagement, "Create a category translation"),
        new(ContentCoreFeatures.CategoryTranslation, AppAction.Update, PermissionGroup.ContentManagement, "Update a category translation"),
        new(ContentCoreFeatures.CategoryTranslation, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a category translation"),

        // ── Specialization ───────────────────────────────────────────────────
        new(ContentCoreFeatures.Specialization, AppAction.Read,   PermissionGroup.ContentManagement, "View specializations"),
        new(ContentCoreFeatures.Specialization, AppAction.Create, PermissionGroup.ContentManagement, "Create a specialization"),
        new(ContentCoreFeatures.Specialization, AppAction.Update, PermissionGroup.ContentManagement, "Update a specialization"),
        new(ContentCoreFeatures.Specialization, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a specialization"),

        // ── Tag ──────────────────────────────────────────────────────────────
        new(ContentCoreFeatures.Tag, AppAction.Read,   PermissionGroup.ContentManagement, "View tags"),
        new(ContentCoreFeatures.Tag, AppAction.Create, PermissionGroup.ContentManagement, "Create a tag"),
        new(ContentCoreFeatures.Tag, AppAction.Update, PermissionGroup.ContentManagement, "Update a tag"),
        new(ContentCoreFeatures.Tag, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a tag"),

        // ── EntityCategory ───────────────────────────────────────────────────
        new(ContentCoreFeatures.EntityCategory, AppAction.Read,   PermissionGroup.ContentManagement, "View entity-category assignments"),
        new(ContentCoreFeatures.EntityCategory, AppAction.Create, PermissionGroup.ContentManagement, "Assign category to entity"),
        new(ContentCoreFeatures.EntityCategory, AppAction.Delete, PermissionGroup.ContentManagement, "Remove category from entity"),

        // ── EntityImage ──────────────────────────────────────────────────────
        new(ContentCoreFeatures.EntityImage, AppAction.Read,   PermissionGroup.ContentManagement, "View entity images"),
        new(ContentCoreFeatures.EntityImage, AppAction.Create, PermissionGroup.ContentManagement, "Upload an entity image"),
        new(ContentCoreFeatures.EntityImage, AppAction.Update, PermissionGroup.ContentManagement, "Update entity image details"),
        new(ContentCoreFeatures.EntityImage, AppAction.Delete, PermissionGroup.ContentManagement, "Delete an entity image"),

        // ── EntityTag ────────────────────────────────────────────────────────
        new(ContentCoreFeatures.EntityTag, AppAction.Read,   PermissionGroup.ContentManagement, "View entity-tag assignments"),
        new(ContentCoreFeatures.EntityTag, AppAction.Create, PermissionGroup.ContentManagement, "Assign tag to entity"),
        new(ContentCoreFeatures.EntityTag, AppAction.Delete, PermissionGroup.ContentManagement, "Remove tag from entity"),

        // ── TranslationCache ─────────────────────────────────────────────────
        new(ContentCoreFeatures.TranslationCache, AppAction.Read,   PermissionGroup.ContentManagement, "View translation cache entries"),
        new(ContentCoreFeatures.TranslationCache, AppAction.Create, PermissionGroup.ContentManagement, "Create a translation cache entry"),
        new(ContentCoreFeatures.TranslationCache, AppAction.Update, PermissionGroup.ContentManagement, "Update a translation cache entry"),
        new(ContentCoreFeatures.TranslationCache, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a translation cache entry"),

        // ── Language ─────────────────────────────────────────────────────────
        new(ContentCoreFeatures.Language, AppAction.Read,   PermissionGroup.ContentManagement, "View languages"),
        new(ContentCoreFeatures.Language, AppAction.Create, PermissionGroup.ContentManagement, "Create a language"),
        new(ContentCoreFeatures.Language, AppAction.Update, PermissionGroup.ContentManagement, "Update a language"),
        new(ContentCoreFeatures.Language, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a language"),

        // ── Attachment ───────────────────────────────────────────────────────
        new(ContentCoreFeatures.Attachment, AppAction.Read,   PermissionGroup.ContentManagement, "View attachments"),
        new(ContentCoreFeatures.Attachment, AppAction.Create, PermissionGroup.ContentManagement, "Upload an attachment"),
        new(ContentCoreFeatures.Attachment, AppAction.Update, PermissionGroup.ContentManagement, "Update attachment details"),
        new(ContentCoreFeatures.Attachment, AppAction.Delete, PermissionGroup.ContentManagement, "Delete an attachment"),

        // ── Promotion (promo/ad placement blocks) ────────────────────────────
        new(ContentCoreFeatures.Promotion, AppAction.Read,   PermissionGroup.ContentManagement, "View promo placements (including inactive)"),
        new(ContentCoreFeatures.Promotion, AppAction.Create, PermissionGroup.ContentManagement, "Create a promo placement"),
        new(ContentCoreFeatures.Promotion, AppAction.Update, PermissionGroup.ContentManagement, "Update a promo placement"),
        new(ContentCoreFeatures.Promotion, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a promo placement"),
    ];
}
