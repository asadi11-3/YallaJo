using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Businesses;
using YallaJo.Web.Areas.Admin.Models.Places;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class BusinessesFacade
{
    private const int PageSize = 20;

    // The place picker is a NAME dropdown (F10). We pull a generous first page of
    // places so the admin can pick by name; there is no name-search endpoint, so a
    // single bounded page is the compliant lookup surface.
    private const int PlaceOptionsPageSize = 200;

    private readonly BusinessesApiClient _api;
    private readonly PlacesApiClient _places;
    private readonly IOutputCacheStore _cache;

    public BusinessesFacade(BusinessesApiClient api, PlacesApiClient places, IOutputCacheStore cache)
    {
        _api = api;
        _places = places;
        _cache = cache;
    }

    public async Task<ApiResult<BusinessesVm>> GetIndexAsync(
        Guid? placeId, string? status, int page, CancellationToken ct = default)
    {
        var hasPlace = placeId is { } pid && pid != Guid.Empty;

        // API1: fetch the place dropdown options concurrently with the businesses page
        // (when a place is selected) instead of awaiting them sequentially.
        var placesTask = _places.ListAsync(1, PlaceOptionsPageSize, new PlaceListFilterVm(), ct);
        var businessesTask = hasPlace
            ? _api.GetByPlaceAsync(placeId!.Value, page, PageSize, ct)
            : null;

        var placesResult = await placesTask;
        if (placesResult.IsUnauthorized)
        {
            return ApiResult<BusinessesVm>.ForceSignOut();
        }

        var placeOptions = (placesResult is { IsSuccess: true, Data: not null }
                ? placesResult.Data.Items
                : [])
            .Select(p => new PlaceOptionVm { Id = p.Id, Name = p.Name })
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var selectedPlaceName = hasPlace
            ? placeOptions.FirstOrDefault(p => p.Id == placeId!.Value)?.Name
            : null;

        if (!hasPlace)
        {
            return ApiResult<BusinessesVm>.Ok(new BusinessesVm
            {
                StatusFilter = status,
                PageNumber = page,
                PageSize = PageSize,
                PlaceOptions = placeOptions,
            });
        }

        var result = await businessesTask!;
        if (result.IsUnauthorized)
        {
            return ApiResult<BusinessesVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<BusinessesVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load businesses.");
        }

        var vm = BusinessesMapper.ToVm(result.Data, placeId, status);
        vm.PlaceOptions = placeOptions;
        vm.SelectedPlaceName = selectedPlaceName;
        return ApiResult<BusinessesVm>.Ok(vm);
    }

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default) =>
        Normalize(_api.ApproveAsync(id, ct), id, "Could not approve the business.");

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct = default) =>
        Normalize(_api.RejectAsync(id, reason, ct), id, "Could not reject the business.");

    public Task<ApiResult> RequestMoreDocsAsync(Guid id, string reason, CancellationToken ct = default) =>
        Normalize(_api.RequestMoreDocsAsync(id, reason, ct), id, "Could not request more documents.");

    public Task<ApiResult> SuspendAsync(Guid id, string reason, CancellationToken ct = default) =>
        Normalize(_api.SuspendAsync(id, reason, ct), id, "Could not suspend the business.");

    public Task<ApiResult> ReinstateAsync(Guid id, CancellationToken ct = default) =>
        Normalize(_api.ReinstateAsync(id, ct), id, "Could not reinstate the business.");

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default) =>
        Normalize(_api.DeleteAsync(id, ct), id, "Could not delete the business.");

    // Evicts the public business:{id} output-cache tag on a successful moderation write (§8.5 C3)
    // so the cached public business detail page reflects the change immediately. Uses
    // CancellationToken.None so eviction still runs if the admin client disconnected.
    private async Task<ApiResult> Normalize(Task<ApiResult> call, Guid businessId, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            await _cache.EvictByTagAsync($"business:{businessId}", CancellationToken.None);
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "Business not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409,
                "This action is not allowed in the business's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
