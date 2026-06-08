using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Accessibility;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessAccessibilityFacade
{
    private readonly AccessibilityApiClient _api;
    private readonly MyBusinessesApiClient _businesses;
    private readonly IOutputCacheStore _cache;

    public BusinessAccessibilityFacade(AccessibilityApiClient api, MyBusinessesApiClient businesses, IOutputCacheStore cache)
    {
        _api = api;
        _businesses = businesses;
        _cache = cache;
    }

    public async Task<ApiResult<AccessibilityVm>> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        var business = await _businesses.GetByIdAsync(businessId, ct);
        if (business.IsUnauthorized)
        {
            return ApiResult<AccessibilityVm>.ForceSignOut();
        }

        if (business is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<AccessibilityVm>.Fail(business.StatusCode, business.Error ?? "Could not load the business.");
        }

        var features = await _api.GetAsync(businessId, ct);
        if (features.IsUnauthorized)
        {
            return ApiResult<AccessibilityVm>.ForceSignOut();
        }

        if (features is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<AccessibilityVm>.Fail(features.StatusCode, features.Error ?? "Could not load accessibility features.");
        }

        var vm = new AccessibilityVm
        {
            BusinessId = businessId,
            BusinessName = business.Data.Name,
            Status = business.Data.Status,
            Features = AccessibilityMapper.ToFeatureRows(features.Data)
        };

        return ApiResult<AccessibilityVm>.Ok(vm);
    }

    public async Task<ApiResult> SaveAsync(Guid businessId, IReadOnlyList<AccessibilityFeatureFormVm> features, CancellationToken ct = default)
    {
        var payload = features
            .Select(f => new AccessibilityFeatureItemApiRequest(
                f.FeatureType,
                f.Name.Trim(),
                NullIfBlank(f.Description),
                f.IsAvailable))
            .ToList();

        var result = await Normalize(_api.SaveAsync(businessId, payload, ct), "Could not save the accessibility features.");
        if (result.IsSuccess) await _cache.EvictByTagAsync($"business:{businessId}", ct);
        return result;
    }

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsForbidden)
        {
            return ApiResult.Fail(403, "You do not own this business.");
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "The business was not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
