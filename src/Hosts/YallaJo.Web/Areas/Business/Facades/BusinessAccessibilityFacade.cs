using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Accessibility;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessAccessibilityFacade
{
    private readonly AccessibilityApiClient _api;
    private readonly MyBusinessesApiClient _businesses;
    private readonly IOutputCacheStore _cache;
    private readonly IStringLocalizer<SharedResource> _l;

    public BusinessAccessibilityFacade(AccessibilityApiClient api, MyBusinessesApiClient businesses, IOutputCacheStore cache, IStringLocalizer<SharedResource> localizer)
    {
        _api = api;
        _businesses = businesses;
        _cache = cache;
        _l = localizer;
    }

    public async Task<ApiResult<AccessibilityVm>> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        // API1/D-16: fetch the business detail and the accessibility features concurrently.
        var businessTask = _businesses.GetByIdAsync(businessId, ct);
        var featuresTask = _api.GetAsync(businessId, ct);
        await Task.WhenAll(businessTask, featuresTask);

        var business = await businessTask;
        if (business.IsUnauthorized)
        {
            return ApiResult<AccessibilityVm>.ForceSignOut();
        }

        if (business is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<AccessibilityVm>.Fail(business.StatusCode, business.Error ?? _l["Business.Error.LoadBusiness"].Value);
        }

        var features = await featuresTask;
        if (features.IsUnauthorized)
        {
            return ApiResult<AccessibilityVm>.ForceSignOut();
        }

        if (features is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<AccessibilityVm>.Fail(features.StatusCode, features.Error ?? _l["Business.Error.LoadAccessibility"].Value);
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

        var result = await Normalize(_api.SaveAsync(businessId, payload, ct), _l["Business.Error.SaveAccessibilityFailed"].Value);
        if (result.IsSuccess) await _cache.EvictByTagAsync($"business:{businessId}", ct);
        return result;
    }

    private async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
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
            return ApiResult.Fail(403, _l["Business.Error.NotOwner"].Value);
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, _l["Business.Error.BusinessNotFound"].Value);
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, _l["Business.Error.StateConflict"].Value);
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
