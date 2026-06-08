using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Business.ApiClients;
using YallaJo.Web.Areas.Business.Models.Amenities;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Business.Facades;

public sealed class BusinessAmenitiesFacade
{
    private readonly AmenitiesApiClient _api;
    private readonly MyBusinessesApiClient _businesses;
    private readonly IOutputCacheStore _cache;

    public BusinessAmenitiesFacade(AmenitiesApiClient api, MyBusinessesApiClient businesses, IOutputCacheStore cache)
    {
        _api = api;
        _businesses = businesses;
        _cache = cache;
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
            return ApiResult<AmenitiesVm>.Fail(business.StatusCode, business.Error ?? "Could not load the business.");
        }

        var amenities = await _api.GetAmenitiesAsync(businessId, ct: ct);
        if (amenities.IsUnauthorized)
        {
            return ApiResult<AmenitiesVm>.ForceSignOut();
        }

        if (amenities is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<AmenitiesVm>.Fail(amenities.StatusCode, amenities.Error ?? "Could not load the amenities.");
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
        var result = await Normalize(_api.AddAsync(businessId, request, ct), "Could not add the amenity.");
        if (result.IsSuccess) await _cache.EvictByTagAsync($"business:{businessId}", ct);
        return result;
    }

    public async Task<ApiResult> RemoveAsync(Guid businessId, Guid amenityId, CancellationToken ct = default)
    {
        var result = await Normalize(_api.RemoveAsync(amenityId, ct), "Could not remove the amenity.");
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
            return ApiResult.Fail(404, "The business or amenity was not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This amenity already exists.");
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
