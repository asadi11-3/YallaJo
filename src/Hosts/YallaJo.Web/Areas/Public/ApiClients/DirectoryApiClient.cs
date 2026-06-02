using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class DirectoryApiClient
{
    private readonly IApiClient _api;

    public DirectoryApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PaginatedBusinessesResponse>> SearchBusinessesAsync(
        string? query, string? businessType, string? city, int page, int pageSize, CancellationToken ct = default)
    {
        var qs = $"?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(query)) qs += $"&query={Uri.EscapeDataString(query)}";
        if (!string.IsNullOrWhiteSpace(businessType)) qs += $"&businessType={Uri.EscapeDataString(businessType)}";
        if (!string.IsNullOrWhiteSpace(city)) qs += $"&city={Uri.EscapeDataString(city)}";
        return _api.GetAsync<PaginatedBusinessesResponse>($"/api/v1/places/businesses/search{qs}", ct);
    }

    public Task<ApiResult<List<DirectoryAttachmentResponse>>> GetAttachmentsAsync(Guid businessId, CancellationToken ct = default)
        => _api.GetAsync<List<DirectoryAttachmentResponse>>($"/api/v1/content-core/attachments?entityType=Business&entityId={businessId}", ct);

    public Task<ApiResult<BusinessDetailResponse>> GetBusinessAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<BusinessDetailResponse>($"/api/v1/places/businesses/{id}", ct);

    public Task<ApiResult<List<BusinessHoursResponse>>> GetHoursAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<BusinessHoursResponse>>($"/api/v1/places/businesses/{id}/hours", ct);

    public Task<ApiResult<List<BusinessAmenityResponse>>> GetAmenitiesAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<BusinessAmenityResponse>>($"/api/v1/places/businesses/{id}/amenities", ct);

    public Task<ApiResult<List<ServiceItemResponse>>> GetServicesAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<ServiceItemResponse>>($"/api/v1/places/businesses/{id}/services", ct);

    public Task<ApiResult<List<AccessibilityFeatureResponse>>> GetAccessibilityAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<AccessibilityFeatureResponse>>($"/api/v1/places/businesses/{id}/accessibility", ct);
}
