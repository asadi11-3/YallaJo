using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.Mappers;
using YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.ViewModels;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places;

public sealed class PlacesFacade
{
    private readonly PlacesApiClient _api;
    public PlacesFacade(PlacesApiClient api) => _api = api;

    // ── Queries ──────────────────────────────────────────────────────────────
    public async Task<ApiResult<PlaceListVm>> GetPlacesAsync(
        int page,
        int pageSize,
        PlaceListFilterVm filter,
        CancellationToken ct = default)
    {
        var result = await _api.ListAsync(page, pageSize, filter, ct);

        if (result.IsSuccess && result.Data is not null)
        {
            var data = result.Data;
            var vm = new PlaceListVm
            {
                Items           = data.Items.Select(PlacesMapper.ToRow).ToList(),
                Filter          = filter,
                Page            = data.PageNumber == 0 ? page     : data.PageNumber,
                PageSize        = data.PageSize   == 0 ? pageSize : data.PageSize,
                TotalCount      = data.TotalCount,
                TotalPages      = data.TotalPages,
                HasPreviousPage = data.HasPreviousPage,
                HasNextPage     = data.HasNextPage,
            };
            return ApiResult<PlaceListVm>.CreateSuccess(vm);
        }

        if (result.IsUnauthorized) return ApiResult<PlaceListVm>.ForceSignOut();
        return ApiResult<PlaceListVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult<PlaceDetailsVm>> GetDetailsAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);

        if (result.IsSuccess && result.Data is not null)
            return ApiResult<PlaceDetailsVm>.CreateSuccess(PlacesMapper.ToDetails(result.Data));

        if (result.IsUnauthorized) return ApiResult<PlaceDetailsVm>.ForceSignOut();
        if (result.IsNotFound)     return ApiResult<PlaceDetailsVm>.CreateFailure(404, "Place not found.");
        return ApiResult<PlaceDetailsVm>.CreateFailure(result.StatusCode, result.Error);
    }

    public async Task<ApiResult<EditPlaceVm>> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);

        if (result.IsSuccess && result.Data is not null)
            return ApiResult<EditPlaceVm>.CreateSuccess(PlacesMapper.ToEditVm(result.Data));

        if (result.IsUnauthorized) return ApiResult<EditPlaceVm>.ForceSignOut();
        if (result.IsNotFound)     return ApiResult<EditPlaceVm>.CreateFailure(404, "Place not found.");
        return ApiResult<EditPlaceVm>.CreateFailure(result.StatusCode, result.Error);
    }

    // ── Commands ─────────────────────────────────────────────────────────────
    public async Task<ApiResult> CreateAsync(CreatePlaceVm vm, CancellationToken ct = default)
    {
        var result = await _api.CreateAsync(PlacesMapper.ToCreateRequest(vm), ct);

        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsForbidden)       return ApiResult.Fail(403, "You don't have permission to create a place.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        if (result.IsConflict)        return ApiResult.Fail(409, PreferDetail(result.Error, "A place with this slug already exists."));
        return ApiResult.Fail(result.StatusCode, PreferDetail(result.Error, "Could not create place."));
    }

    public async Task<ApiResult> UpdateAsync(EditPlaceVm vm, CancellationToken ct = default)
        => Normalize(
            await _api.UpdateAsync(vm.Id, PlacesMapper.ToUpdateRequest(vm), ct),
            "Could not update place.");

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => Normalize(await _api.DeleteAsync(id, ct), "Could not delete place.");

    public async Task<ApiResult> FeatureAsync(Guid id, bool featured, CancellationToken ct = default)
        => Normalize(
            await _api.FeatureAsync(id, featured, ct),
            featured ? "Could not feature place." : "Could not unfeature place.");

    public async Task<ApiResult> VerifyAsync(Guid id, bool verified, CancellationToken ct = default)
        => Normalize(
            await _api.VerifyAsync(id, verified, ct),
            verified ? "Could not verify place." : "Could not unverify place.");

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)         return ApiResult.Ok();
        if (result.IsUnauthorized)    return ApiResult.ForceSignOut();
        if (result.IsForbidden)       return ApiResult.Fail(403, PreferDetail(result.Error, "You don't have permission to perform this action."));
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        if (result.IsConflict)        return ApiResult.Fail(409, PreferDetail(result.Error, "Conflict: a place with this slug already exists."));
        if (result.IsNotFound)        return ApiResult.Fail(404, PreferDetail(result.Error, "Place not found."));
        return ApiResult.Fail(result.StatusCode, PreferDetail(result.Error, fallback));
    }

    private static string PreferDetail(string? apiError, string fallback) =>
        string.IsNullOrWhiteSpace(apiError) ? fallback : apiError!;
}
