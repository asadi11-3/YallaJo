namespace ContentSeo.Presentation.Endpoints.Sitemap.Models;

public sealed record UpdateSitemapEntryRequest(decimal? Priority, string? ChangeFrequency);
