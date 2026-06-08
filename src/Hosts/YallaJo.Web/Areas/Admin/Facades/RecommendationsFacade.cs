using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Recommendations;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class RecommendationsFacade
{
    private readonly RecommendationsApiClient _api;
    private readonly IOutputCacheStore _cache;

    public RecommendationsFacade(RecommendationsApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<ApiResult<RecommendationsVm>> GetIndexAsync(CancellationToken ct = default)
    {
        var result = await _api.GetBatchesAsync(ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<RecommendationsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<RecommendationsVm>.Fail(result.StatusCode, result.Error ?? "Could not load recommendation batches.");
        }

        return ApiResult<RecommendationsVm>.Ok(RecommendationsMapper.ToVm(result.Data));
    }

    public Task<ApiResult> RefreshBatchesAsync(RefreshBatchRequest req, CancellationToken ct = default)
        => Normalize(_api.RefreshBatchesAsync(req, ct), "Could not refresh the recommendation batches.", "homepage");

    public Task<ApiResult> CreateBoostAsync(CreateBoostRequest req, CancellationToken ct = default)
        => Normalize(_api.CreateBoostAsync(req, ct), "Could not create the boost package.", EntityTags(req.EntityKind, req.EntityId));

    public Task<ApiResult> DeactivateBoostAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.DeactivateBoostAsync(id, ct), "Could not deactivate the boost package.", "homepage");

    public Task<ApiResult> CreatePinAsync(CreatePinRequest req, CancellationToken ct = default)
        => Normalize(_api.CreatePinAsync(req, ct), "Could not create the editorial pin.", EntityTags(req.EntityKind, req.EntityId));

    public Task<ApiResult> DeactivatePinAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.DeactivatePinAsync(id, ct), "Could not deactivate the editorial pin.", "homepage");

    // Boosts/pins surface on the homepage rails (§8.8 C3) → always evict "homepage"; on create we
    // also know the boosted/pinned entity, so evict its public detail tag (tour:{id} or place:{id}).
    private static string[] EntityTags(string? entityKind, Guid entityId)
    {
        var entityTag = string.Equals(entityKind, "tour", StringComparison.OrdinalIgnoreCase)
            ? $"tour:{entityId}"
            : string.Equals(entityKind, "place", StringComparison.OrdinalIgnoreCase)
                ? $"place:{entityId}"
                : null;
        return entityTag is null ? ["homepage"] : ["homepage", entityTag];
    }

    // Evicts the supplied public output-cache tags on a successful write (§8.8 C3). Uses
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

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "Not found.");
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
}
