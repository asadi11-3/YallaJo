using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class AdminToursFacade
{
    private readonly AdminToursApiClient _api;
    private readonly PlacesApiClient _placesApi;
    private readonly IOutputCacheStore _cache;

    public AdminToursFacade(AdminToursApiClient api, PlacesApiClient placesApi, IOutputCacheStore cache)
    {
        _api = api;
        _placesApi = placesApi;
        _cache = cache;
    }

    public async Task<ApiResult<AdminToursIndexVm>> GetListAsync(
        string? status, int page, int pageSize, string? sort, CancellationToken ct = default)
    {
        var result = await _api.ListAsync(status, page, pageSize, sort, ct);
        if (result.IsUnauthorized) return ApiResult<AdminToursIndexVm>.ForceSignOut();
        if (result.IsValidationError)
            return ApiResult<AdminToursIndexVm>.Fail(result.StatusCode, result.Error ?? "Invalid filter.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<AdminToursIndexVm>.Fail(result.StatusCode, result.Error ?? "Could not load tours.");

        return ApiResult<AdminToursIndexVm>.Ok(AdminToursMapper.ToIndexVm(result.Data, status));
    }

    public async Task<ApiResult<AdminTourDetailsVm>> GetDetailsAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetByIdAsync(id, ct);
        if (result.IsUnauthorized) return ApiResult<AdminTourDetailsVm>.ForceSignOut();
        if (result.IsNotFound) return ApiResult<AdminTourDetailsVm>.Fail(404, "Tour not found.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<AdminTourDetailsVm>.Fail(result.StatusCode, result.Error ?? "Could not load the tour.");

        var vm = AdminToursMapper.ToDetailsVm(result.Data);
        await HydratePlaceAsync(vm, ct);
        return ApiResult<AdminTourDetailsVm>.Ok(vm);
    }

    private async Task HydratePlaceAsync(AdminTourDetailsVm vm, CancellationToken ct)
    {
        if (vm.PlaceId is not { } placeId) return;

        try
        {
            var place = await _placesApi.GetByIdAsync(placeId, ct);
            if (place is { IsSuccess: true, Data: { } p })
            {
                vm.PlaceName = p.Name;
                vm.PlaceCity = p.City;
                vm.PlaceCountry = p.Country;
            }
            else
            {
                vm.PlaceLookupFailed = true;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            vm.PlaceLookupFailed = true;
        }
    }

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.ApproveAsync(id, rv, ct), "Could not approve the tour.", ct, $"tour:{id}");

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.RejectAsync(id, rv, reason, ct), "Could not reject the tour.", ct, $"tour:{id}");

    public Task<ApiResult> SuspendAsync(Guid id, string reason, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.SuspendAsync(id, rv, reason, ct), "Could not suspend the tour.", ct, $"tour:{id}");

    public Task<ApiResult> ReinstateAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.ReinstateAsync(id, rv, ct), "Could not reinstate the tour.", ct, $"tour:{id}");

    // ── §8.4 moderation extras (no optimistic-concurrency token required) ─────────

    public Task<ApiResult> FeatureAsync(Guid id, bool isFeatured, CancellationToken ct = default)
        => Normalize(_api.FeatureAsync(id, isFeatured, ct),
            isFeatured ? "Could not feature the tour." : "Could not unfeature the tour.", $"tour:{id}");

    public Task<ApiResult> ApproveProposalAsync(Guid id, bool isExclusive, CancellationToken ct = default)
        => Normalize(_api.ApproveProposalAsync(id, isExclusive, ct), "Could not approve the proposal.");

    public Task<ApiResult> RejectProposalAsync(Guid id, string reason, CancellationToken ct = default)
        => Normalize(_api.RejectProposalAsync(id, reason, ct), "Could not reject the proposal.");

    public Task<ApiResult> ApprovePackageAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.ApprovePackageAsync(id, ct), "Could not approve the package.");

    public Task<ApiResult> RejectPackageAsync(Guid id, string reason, CancellationToken ct = default)
        => Normalize(_api.RejectPackageAsync(id, reason, ct), "Could not reject the package.");

    public Task<ApiResult> SuspendOfferingAsync(Guid tourId, Guid guideId, string reason, CancellationToken ct = default)
        => Normalize(_api.SuspendOfferingAsync(tourId, guideId, reason, ct), "Could not suspend the guide offering.", $"tour:{tourId}");

    public Task<ApiResult> ReinstateOfferingAsync(Guid tourId, Guid guideId, CancellationToken ct = default)
        => Normalize(_api.ReinstateOfferingAsync(tourId, guideId, ct), "Could not reinstate the guide offering.", $"tour:{tourId}");


    private async Task<ApiResult> WithRowVersion(
        Guid id, Func<byte[], Task<ApiResult>> mutate, string fallback, CancellationToken ct, params string[] evictTags)
    {
        var detail = await _api.GetByIdAsync(id, ct);
        if (detail.IsUnauthorized) return ApiResult.ForceSignOut();
        if (detail.IsNotFound) return ApiResult.Fail(404, "Tour not found.");
        if (!detail.IsSuccess || detail.Data is null)
            return ApiResult.Fail(detail.StatusCode, detail.Error ?? fallback);

        if (detail.Data.RowVersion is not { Length: > 0 })
            return ApiResult.Fail(409, "This tour is missing concurrency data. Please reload and try again.");

        return await Normalize(mutate(detail.Data.RowVersion), fallback, evictTags);
    }

    // Evicts the public tour:{id} output-cache tag(s) on a successful moderation write so the
    // cached public tour detail page reflects the change immediately (§8.4 C3). Uses
    // CancellationToken.None so eviction still runs if the admin client disconnected.
    private async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback, params string[] evictTags)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            foreach (var tag in evictTags)
                await _cache.EvictByTagAsync(tag, CancellationToken.None);
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "Tour not found.");
        if (result.IsConflict)
            return ApiResult.Fail(409,
                "This tour was modified by someone else, or the action is not allowed in its current state. Please reload and try again.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors ?? EmptyErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>();
}
