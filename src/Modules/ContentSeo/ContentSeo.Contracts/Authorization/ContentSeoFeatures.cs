namespace ContentSeo.Contracts.Authorization;

/// <summary>
/// Feature string constants owned by the ContentSeo bounded context.
/// Used as the first argument to <c>MustHavePermissionAttribute</c> on endpoints,
/// and as the <c>Feature</c> column for <see cref="ContentSeoPermissionCatalog"/> rows.
/// </summary>
public static class ContentSeoFeatures
{
    public const string SeoMetadata = nameof(SeoMetadata);
    public const string Redirect    = nameof(Redirect);
    public const string Sitemap     = nameof(Sitemap);
    public const string FaqItem     = nameof(FaqItem);
    public const string Weather     = nameof(Weather);
}
