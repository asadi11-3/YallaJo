using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Infrastructure.Api.Contracts;
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
    private readonly IApiAssetUrlResolver _assetResolver;

    public DirectoryFacade(DirectoryApiClient api, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
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

        var hoursTask = SafeListAsync(() => _api.GetHoursAsync(id, ct));
        var amenitiesTask = SafeListAsync(() => _api.GetAmenitiesAsync(id, ct));
        var servicesTask = SafeListAsync(() => _api.GetServicesAsync(id, ct));
        var accessibilityTask = SafeListAsync(() => _api.GetAccessibilityAsync(id, ct));

        await Task.WhenAll(hoursTask, amenitiesTask, servicesTask, accessibilityTask);

        // Business detail exposes no public gallery field and the attachment endpoint
        // is not anonymous-accessible, so use a single deterministic placeholder
        // (temporary public image API gap — see PublicImagePlaceholder).
        var imageUrls = new List<string> { PublicImagePlaceholder.ResolveBusinessImage(id) };

        // Prefer dedicated hours endpoint, fall back to the embedded list.
        var hours = hoursTask.Result.Count > 0 ? hoursTask.Result : d.BusinessHours;

        var vm = new BusinessDetailVm
        {
            Id = d.Id,
            Name = d.Name,
            Slug = d.Slug,
            Description = d.Description,
            BusinessType = d.BusinessType,
            Address = d.Address,
            City = d.City,
            Country = d.Country,
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

        return ApiResult<BusinessDetailVm>.Ok(vm);
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
