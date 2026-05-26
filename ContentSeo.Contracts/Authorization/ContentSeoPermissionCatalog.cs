using YallaJo.SharedKernel.Application.Authorization;

namespace ContentSeo.Contracts.Authorization;

/// <summary>
/// Permission catalog for the ContentSeo bounded context.
/// 15 permissions across 5 features (SeoMetadata, Redirect, Sitemap, FaqItem, Weather).
/// Registered in <c>ContentSeo.Infrastructure.DependencyInjection</c> — discovered
/// automatically by <c>PermissionSeeder</c> via the DI <see cref="IEnumerable{T}"/>
/// of <see cref="IPermissionCatalog"/>.
/// </summary>
public sealed class ContentSeoPermissionCatalog : IPermissionCatalog
{
    public string ModuleName => "ContentSeo";

    public IReadOnlyList<PermissionDescriptor> Permissions { get; } =
    [
        // ── SeoMetadata (2) ───────────────────────────────────────────────────
        // Note: Read/Delete not used by any endpoint (upsert pattern — no separate read/delete)
        new(ContentSeoFeatures.SeoMetadata, AppAction.Create, PermissionGroup.ContentManagement, "Create SEO metadata"),
        new(ContentSeoFeatures.SeoMetadata, AppAction.Update, PermissionGroup.ContentManagement, "Update SEO metadata"),

        // ── Redirect (4) ──────────────────────────────────────────────────────
        new(ContentSeoFeatures.Redirect, AppAction.Read,   PermissionGroup.ContentManagement, "View redirects"),
        new(ContentSeoFeatures.Redirect, AppAction.Create, PermissionGroup.ContentManagement, "Create a redirect"),
        new(ContentSeoFeatures.Redirect, AppAction.Update, PermissionGroup.ContentManagement, "Update a redirect"),
        new(ContentSeoFeatures.Redirect, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a redirect"),

        // ── Sitemap (3) ───────────────────────────────────────────────────────
        // Note: Create not used (sitemap entries auto-generated from entity events)
        new(ContentSeoFeatures.Sitemap, AppAction.Read,   PermissionGroup.ContentManagement, "View sitemap entries"),
        new(ContentSeoFeatures.Sitemap, AppAction.Update, PermissionGroup.ContentManagement, "Update a sitemap entry"),
        new(ContentSeoFeatures.Sitemap, AppAction.Delete, PermissionGroup.ContentManagement, "Delete a sitemap entry"),
        new(ContentSeoFeatures.Sitemap, AppAction.Refresh, PermissionGroup.ContentManagement, "Trigger sitemap regeneration / submission"),

        // ── FaqItem (3) ───────────────────────────────────────────────────────
        // Note: Read not used (FAQ items are public, no auth required for reads)
        new(ContentSeoFeatures.FaqItem, AppAction.Create, PermissionGroup.ContentManagement, "Create an FAQ item"),
        new(ContentSeoFeatures.FaqItem, AppAction.Update, PermissionGroup.ContentManagement, "Update an FAQ item"),
        new(ContentSeoFeatures.FaqItem, AppAction.Delete, PermissionGroup.ContentManagement, "Delete an FAQ item"),

        // ── Weather (3) ───────────────────────────────────────────────────────
        // Note: Read not used (weather data is public, no auth required for reads)
        new(ContentSeoFeatures.Weather, AppAction.Refresh, PermissionGroup.ContentManagement, "Force a weather refresh"),
        new(ContentSeoFeatures.Weather, AppAction.Update, PermissionGroup.ContentManagement, "Update weather cache settings"),
        new(ContentSeoFeatures.Weather, AppAction.Delete, PermissionGroup.ContentManagement, "Invalidate weather cache"),
    ];
}
