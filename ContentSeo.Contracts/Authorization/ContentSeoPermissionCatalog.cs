using YallaJo.SharedKernel.Application.Authorization;

namespace ContentSeo.Contracts.Authorization;

/// <summary>
/// Permission catalog for the ContentSeo bounded context.
/// Registered in <c>ContentSeo.Infrastructure.DependencyInjection</c> — discovered
/// automatically by <c>PermissionSeeder</c> via the DI <see cref="IEnumerable{T}"/>
/// of <see cref="IPermissionCatalog"/>.
/// </summary>
public sealed class ContentSeoPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "ContentSeo";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── SeoMetadata ───────────────────────────────────────────────────────
        new(ContentSeoFeatures.SeoMetadata, AppAction.Read,   PermissionGroup.ContentManagement, "View SEO metadata"),
        new(ContentSeoFeatures.SeoMetadata, AppAction.Create, PermissionGroup.ContentManagement, "Create SEO metadata"),
        new(ContentSeoFeatures.SeoMetadata, AppAction.Update, PermissionGroup.ContentManagement, "Update SEO metadata"),
        new(ContentSeoFeatures.SeoMetadata, AppAction.Delete, PermissionGroup.ContentManagement, "Delete SEO metadata"),

        // ── Redirect ──────────────────────────────────────────────────────────
        new(ContentSeoFeatures.Redirect, AppAction.Read,   PermissionGroup.ContentManagement, "View redirects"),
        new(ContentSeoFeatures.Redirect, AppAction.Create, PermissionGroup.ContentManagement, "Create a redirect"),
        new(ContentSeoFeatures.Redirect, AppAction.Update, PermissionGroup.ContentManagement, "Update a redirect"),
        new(ContentSeoFeatures.Redirect, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a redirect"),

        // ── Sitemap ───────────────────────────────────────────────────────────
        new(ContentSeoFeatures.Sitemap, AppAction.Read,   PermissionGroup.ContentManagement, "View sitemap entries"),
        new(ContentSeoFeatures.Sitemap, AppAction.Create, PermissionGroup.ContentManagement, "Create a sitemap entry"),
        new(ContentSeoFeatures.Sitemap, AppAction.Update, PermissionGroup.ContentManagement, "Update a sitemap entry"),
        new(ContentSeoFeatures.Sitemap, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a sitemap entry"),
        new(ContentSeoFeatures.Sitemap, AppAction.Refresh, PermissionGroup.ContentManagement, "Trigger sitemap regeneration / submission"),

        // ── FaqItem ───────────────────────────────────────────────────────────
        new(ContentSeoFeatures.FaqItem, AppAction.Read,   PermissionGroup.ContentManagement, "View FAQ items"),
        new(ContentSeoFeatures.FaqItem, AppAction.Create, PermissionGroup.ContentManagement, "Create an FAQ item"),
        new(ContentSeoFeatures.FaqItem, AppAction.Update, PermissionGroup.ContentManagement, "Update an FAQ item"),
        new(ContentSeoFeatures.FaqItem, AppAction.Delete, PermissionGroup.ContentManagement, "Delete an FAQ item"),

        // ── Weather ───────────────────────────────────────────────────────────
        new(ContentSeoFeatures.Weather, AppAction.Read,   PermissionGroup.ContentManagement, "View weather cache entries"),
        new(ContentSeoFeatures.Weather, AppAction.Refresh, PermissionGroup.ContentManagement, "Force a weather refresh"),
        new(ContentSeoFeatures.Weather, AppAction.Update, PermissionGroup.ContentManagement, "Update weather cache settings"),
        new(ContentSeoFeatures.Weather, AppAction.Delete, PermissionGroup.ContentManagement, "Invalidate weather cache"),
    ];
}
