using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Amenities;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessAmenitiesFacade
{
    private readonly AmenitiesApiClient _api;
    private readonly MyBusinessesApiClient _businesses;
    private readonly IOutputCacheStore _cache;
    private readonly IStringLocalizer<SharedResource> _l;

    public BusinessAmenitiesFacade(AmenitiesApiClient api, MyBusinessesApiClient businesses, IOutputCacheStore cache, IStringLocalizer<SharedResource> localizer)
    {
        _api = api;
        _businesses = businesses;
        _cache = cache;
        _l = localizer;
    }

    public async Task<ApiResult<AmenitiesVm>> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        var business = await _businesses.GetByIdAsync(businessId, ct);
        if (business.IsUnauthorized)
        {
            return ApiResult<AmenitiesVm>.ForceSignOut();
        }

        if (business is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<AmenitiesVm>.Fail(business.StatusCode, business.Error ?? _l["Business.Error.LoadBusiness"].Value);
        }

        var amenities = await _api.GetAmenitiesAsync(businessId, ct: ct);
        if (amenities.IsUnauthorized)
        {
            return ApiResult<AmenitiesVm>.ForceSignOut();
        }

        if (amenities is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<AmenitiesVm>.Fail(amenities.StatusCode, amenities.Error ?? _l["Business.Error.LoadAmenities"].Value);
        }

        var vm = new AmenitiesVm
        {
            BusinessId = businessId,
            BusinessName = business.Data.Name,
            Status = business.Data.Status,
            Amenities = AmenitiesMapper.ToRows(amenities.Data),
        };

        return ApiResult<AmenitiesVm>.Ok(vm);
    }

    public async Task<ApiResult> AddAsync(Guid businessId, AddAmenityFormVm form, CancellationToken ct = default)
    {
        var request = new AddBusinessAmenityApiRequest(form.Name.Trim(), NullIfBlank(form.Icon), form.SortOrder);
        var result = await Normalize(_api.AddAsync(businessId, request, ct), _l["Business.Error.AddAmenityFailed"].Value);
        if (result.IsSuccess) await _cache.EvictByTagAsync($"business:{businessId}", ct);
        return result;
    }

    public async Task<ApiResult> RemoveAsync(Guid businessId, Guid amenityId, CancellationToken ct = default)
    {
        var result = await Normalize(_api.RemoveAsync(amenityId, ct), _l["Business.Error.RemoveAmenityFailed"].Value);
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
            return ApiResult.Fail(404, _l["Business.Error.AmenityNotFound"].Value);
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, _l["Business.Error.AmenityExists"].Value);
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
