using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Recommendations;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class RecommendationsFacade
{
    private readonly RecommendationsApiClient _api;

    public RecommendationsFacade(RecommendationsApiClient api) => _api = api;

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
        => Normalize(_api.RefreshBatchesAsync(req, ct), "Could not refresh the recommendation batches.");

    public Task<ApiResult> CreateBoostAsync(CreateBoostRequest req, CancellationToken ct = default)
        => Normalize(_api.CreateBoostAsync(req, ct), "Could not create the boost package.");

    public Task<ApiResult> DeactivateBoostAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.DeactivateBoostAsync(id, ct), "Could not deactivate the boost package.");

    public Task<ApiResult> CreatePinAsync(CreatePinRequest req, CancellationToken ct = default)
        => Normalize(_api.CreatePinAsync(req, ct), "Could not create the editorial pin.");

    public Task<ApiResult> DeactivatePinAsync(Guid id, CancellationToken ct = default)
        => Normalize(_api.DeactivatePinAsync(id, ct), "Could not deactivate the editorial pin.");

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
