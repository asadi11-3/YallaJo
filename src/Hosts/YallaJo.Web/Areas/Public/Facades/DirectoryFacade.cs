using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Areas.Public.Translations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Seo;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class DirectoryFacade
{
    private const int PageSize = 12;

    private static readonly (string Value, string Label)[] SurfacedTypes =
    [
        ("Restaurant", "Restaurants"),
        ("Hotel", "Hotels"),
        ("Shop", "Shops"),
        ("Agency", "Agencies"),
        ("Transport", "Transport"),
        ("Guide", "Guides"),
        ("Activity", "Activities"),
        ("Other", "Other"),
    ];

    private readonly DirectoryApiClient _api;
    private readonly SeoApiClient _seo;
    private readonly TranslationsApiClient _translations;
    private readonly IApiAssetUrlResolver _assetResolver;

    public DirectoryFacade(DirectoryApiClient api, SeoApiClient seo, TranslationsApiClient translations, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _seo = seo;
        _translations = translations;
        _assetResolver = assetResolver;
    }

    public static IReadOnlyList<BusinessTypeOptionVm> BusinessTypeOptions =>
        SurfacedTypes.Select(t => new BusinessTypeOptionVm { Value = t.Value, Label = t.Label }).ToList();

    public static string? NormalizeBusinessType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = SurfacedTypes.FirstOrDefault(t => string.Equals(t.Value, value, StringComparison.OrdinalIgnoreCase));
        return match.Value;
    }

    public async Task<ApiResult<DirectoryVm>> GetDirectoryAsync(
        string? query, string? businessType, string? city, int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        var type = NormalizeBusinessType(businessType);

        var result = await _api.SearchBusinessesAsync(query, type, city, page, PageSize, ct);
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<DirectoryVm>.Fail(result.StatusCode, result.Error ?? "Could not load the directory.");

        var data = result.Data;
        var cards = data.Items.Select(BuildCard).ToList();

        var vm = new DirectoryVm
        {
            Businesses = cards,
            BusinessTypes = BusinessTypeOptions,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalCount = data.TotalCount,
            TotalPages = data.TotalPages,
            HasPreviousPage = data.HasPreviousPage,
            HasNextPage = data.HasNextPage,
            Query = query,
            SelectedBusinessType = type,
            City = city,
        };
        return ApiResult<DirectoryVm>.Ok(vm);
    }

    public async Task<ApiResult<BusinessDetailVm>> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var detailResult = await _api.GetBusinessAsync(id, ct);
        if (!detailResult.IsSuccess || detailResult.Data is null)
            return ApiResult<BusinessDetailVm>.Fail(detailResult.StatusCode, detailResult.Error ?? "Business not found.");

        var d = detailResult.Data;
        var languageCode = TranslationOverlay.ActiveLanguageCode;
        var translationsTask = TranslationOverlay.GetApprovedAsync(_translations, "Business", d.Id, languageCode, ct);

        var hoursTask = SafeListAsync(() => _api.GetHoursAsync(id, ct));
        var amenitiesTask = SafeListAsync(() => _api.GetAmenitiesAsync(id, ct));
        var servicesTask = SafeListAsync(() => _api.GetServicesAsync(id, ct));
        var accessibilityTask = SafeListAsync(() => _api.GetAccessibilityAsync(id, ct));

        await Task.WhenAll(hoursTask, amenitiesTask, servicesTask, accessibilityTask, translationsTask);
        var translations = translationsTask.Result;

        // Business detail exposes no public gallery field and the attachment endpoint
        // is not anonymous-accessible, so use a single deterministic placeholder
        // (temporary public image API gap — see PublicImagePlaceholder).
        var imageUrls = new List<string> { PublicImagePlaceholder.ResolveBusinessImage(id) };

        // Prefer dedicated hours endpoint, fall back to the embedded list.
        var hours = hoursTask.Result.Count > 0 ? hoursTask.Result : d.BusinessHours;

        var seo = TranslationOverlay.ApplySeo(
            await GetSeoAsync(d, ct), translations, languageCode,
            fallbackTitle: d.Name,
            fallbackDescription: d.Description);

        var vm = new BusinessDetailVm
        {
            Id = d.Id,
            PlaceId = d.PlaceId,
            Seo = seo,
            Name = TranslationOverlay.Apply(translations, "Name", d.Name, languageCode) ?? d.Name,
            Slug = d.Slug,
            Description = TranslationOverlay.Apply(translations, "Description", d.Description, languageCode),
            BusinessType = TranslationOverlay.Apply(translations, "BusinessType", d.BusinessType, languageCode) ?? d.BusinessType,
            Address = TranslationOverlay.Apply(translations, "Address", d.Address, languageCode),
            City = TranslationOverlay.Apply(translations, "City", d.City, languageCode),
            Country = TranslationOverlay.Apply(translations, "Country", d.Country, languageCode),
            Phone = d.Phone,
            Email = d.Email,
            Website = d.Website,
            Latitude = d.Latitude,
            Longitude = d.Longitude,
            AverageRating = d.AverageRating,
            ReviewCount = d.ReviewCount,
            IsVerified = d.IsVerified,
            IsFeatured = d.IsFeatured,
            ImageUrls = imageUrls,
            Hours = hours.Select(h => new BusinessHoursVm
            {
                DayOfWeek = h.DayOfWeek,
                OpenTime = h.OpenTime,
                CloseTime = h.CloseTime,
                IsClosed = h.IsClosed,
            }).ToList(),
            Amenities = amenitiesTask.Result
                .OrderBy(a => a.SortOrder)
                .Select(a => new BusinessAmenityVm { Name = a.Name, Icon = a.Icon })
                .ToList(),
            Services = servicesTask.Result
                .Select(s => new BusinessServiceVm
                {
                    Name = s.Name,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    Currency = s.Currency,
                })
                .ToList(),
            AccessibilityFeatures = accessibilityTask.Result
                .Select(a => new AccessibilityFeatureVm
                {
                    Name = a.Name,
                    Description = a.Description,
                    IsAvailable = a.IsAvailable,
                })
                .ToList(),
        };

        // Place-contextual weather widget (master plan §0.2): only when the business
        // is linked to a Place. Best-effort — never fails the page.
        if (d.PlaceId is Guid placeId)
        {
            vm.Weather = await GetWeatherAsync(placeId, ct);
        }

        return ApiResult<BusinessDetailVm>.Ok(vm);
    }

    // Best-effort Place-contextual weather; tolerates API errors, only rethrows on cancellation.
    private async Task<WeatherResponse?> GetWeatherAsync(Guid placeId, CancellationToken ct)
    {
        try
        {
            var result = await _seo.GetWeatherAsync(placeId, ct);
            if (result is { IsSuccess: true, Data: { } weather })
                return weather;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Weather is best-effort; never break the page.
        }

        return null;
    }

    // Best-effort SEO enrichment for the business detail page (SeoEntityType.Business).
    // Never fails the page: tolerates API errors, only rethrows on cancellation.
    private async Task<SeoContent> GetSeoAsync(BusinessDetailResponse d, CancellationToken ct)
    {
        string? metaTitle = null;
        var metaDescription = d.Description;
        string? canonical = null;
        var ogImage = PublicImagePlaceholder.ResolveBusinessImage(d.Id);
        IReadOnlyList<SeoFaqItem> faqs = [];

        try
        {
            var metaTask = _seo.GetMetadataAsync(SeoEntityType.Business, d.Id, ct);
            var faqTask = _seo.GetFaqAsync(SeoEntityType.Business, d.Id, ct);
            await Task.WhenAll(metaTask, faqTask);

            if (metaTask.Result is { IsSuccess: true, Data: { } meta })
            {
                if (!string.IsNullOrWhiteSpace(meta.Title)) metaTitle = meta.Title;
                if (!string.IsNullOrWhiteSpace(meta.Description)) metaDescription = meta.Description;
                canonical = meta.Canonical;
                if (!string.IsNullOrWhiteSpace(meta.OgImage)) ogImage = meta.OgImage;
            }

            if (faqTask.Result is { IsSuccess: true, Data: { } items })
            {
                faqs = items
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new SeoFaqItem { Question = f.Question, Answer = f.Answer })
                    .ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // SEO is best-effort; never break the page.
        }

        return new SeoContent
        {
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            Canonical = canonical,
            OgImage = ogImage,
            Faqs = faqs,
        };
    }

    private static async Task<List<T>> SafeListAsync<T>(Func<Task<ApiResult<List<T>>>> call)
    {
        try
        {
            var result = await call();
            if (result is { IsSuccess: true, Data: { } data })
                return data;
        }
        catch
        {
            // tolerate hydration failure
        }

        return [];
    }

    private BusinessCardVm BuildCard(BusinessSummaryResponse b)
    {
        // Prefer the real public image field; otherwise fall back to a deterministic
        // theme placeholder. Anonymous pages must NOT call the protected attachment
        // endpoint (temporary public image API gap — see PublicImagePlaceholder).
        var imageUrl = !string.IsNullOrWhiteSpace(b.PrimaryImageUrl)
            ? _assetResolver.Resolve(b.PrimaryImageUrl)
            : PublicImagePlaceholder.ResolveBusinessImage(b.Id);

        return new BusinessCardVm
        {
            Id = b.Id,
            Name = b.Name,
            Slug = b.Slug,
            ImageUrl = imageUrl,
            BusinessType = b.BusinessType,
            Location = JoinLocation(b.City, b.Country),
            AverageRating = b.AverageRating,
            ReviewCount = b.ReviewCount,
            IsVerified = b.IsVerified,
            IsFeatured = b.IsFeatured,
        };
    }

    private static string? JoinLocation(string? city, string? country)
    {
        var parts = new[] { city, country }.Where(p => !string.IsNullOrWhiteSpace(p));
        var joined = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }
}
