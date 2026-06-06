using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Infrastructure.Seo;

/// <summary>
/// The six entity types that carry SEO metadata + FAQ on their public detail pages.
/// Values MUST match the backend SeoEntityType enum exactly (Place=0 .. Creator=5).
/// The API route uses the enum NAME (e.g. /api/v1/seo/metadata/Place/{id}).
/// </summary>
public enum SeoEntityType
{
    Place = 0,
    Tour = 1,
    Business = 2,
    Blog = 3,
    TourGuide = 4,
    Creator = 5,
}

/// <summary>Raw SEO metadata payload from GET /api/v1/seo/metadata/{entityType}/{id}.</summary>
public sealed class SeoMetadataResponse
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Canonical { get; init; }
    public string? OgImage { get; init; }
    public string? Keywords { get; init; }
}

/// <summary>Raw FAQ item from GET /api/v1/seo/faq/{entityType}/{id}.</summary>
public sealed class SeoFaqItemResponse
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

/// <summary>A single rendered FAQ entry for a detail page.</summary>
public sealed class SeoFaqItem
{
    public required string Question { get; init; }
    public required string Answer { get; init; }
}

/// <summary>
/// View-model carrier for SEO content attached to a detail view-model.
/// Populated best-effort by a facade; never fails the page when the SEO API is unavailable.
/// </summary>
public sealed class SeoContent
{
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string? Canonical { get; init; }
    public string? OgImage { get; init; }
    public IReadOnlyList<SeoFaqItem> Faqs { get; init; } = [];

    public bool HasFaqs => Faqs.Count > 0;
}

/// <summary>
/// Place-contextual weather payload from GET /api/v1/seo/weather/{placeId}.
/// All fields nullable/tolerant; rendered as a small Place-scoped widget (master plan §0.2).
/// </summary>
public sealed class WeatherResponse
{
    public decimal? Temperature { get; init; }
    public decimal? FeelsLike { get; init; }
    public int? Humidity { get; init; }
    public decimal? WindSpeed { get; init; }
    public int? WindDirection { get; init; }
    public string? Condition { get; init; }
    public string? Icon { get; init; }
    public decimal? UvIndex { get; init; }
    public DateTime FetchedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsStale { get; init; }
    public string? StaleHumanLabel { get; init; }

    public bool HasTemperature => Temperature.HasValue;
}

/// <summary>
/// Shared SEO read client for the six SEO-bearing detail pages
/// (Place / Tour / Business / Blog / TourGuide / Creator) plus the
/// Place-contextual weather widget.
/// Auto-registered Scoped via the *ApiClient suffix convention.
/// </summary>
public sealed class SeoApiClient(IApiClient api)
{
    private const string Base = "/api/v1/seo";

    public Task<ApiResult<SeoMetadataResponse>> GetMetadataAsync(
        SeoEntityType entityType, Guid id, CancellationToken ct = default) =>
        api.GetAsync<SeoMetadataResponse>($"{Base}/metadata/{entityType}/{id}", ct);

    public Task<ApiResult<List<SeoFaqItemResponse>>> GetFaqAsync(
        SeoEntityType entityType, Guid id, CancellationToken ct = default) =>
        api.GetAsync<List<SeoFaqItemResponse>>($"{Base}/faq/{entityType}/{id}", ct);

    /// <summary>Place-contextual current weather (GET /api/v1/seo/weather/{placeId}).</summary>
    public Task<ApiResult<WeatherResponse>> GetWeatherAsync(
        Guid placeId, CancellationToken ct = default) =>
        api.GetAsync<WeatherResponse>($"{Base}/weather/{placeId}", ct);
}
