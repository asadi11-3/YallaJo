using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Commissions;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class CommissionsFacade(CommissionsApiClient api)
{
    private readonly CommissionsApiClient _api = api;

    public async Task<ApiResult<CommissionsVm>> GetIndexAsync(
        string? tier, string? currency, bool includeInactive, CancellationToken ct = default)
    {
        var result = await _api.GetRulesAsync(tier, currency, includeInactive, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<CommissionsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<CommissionsVm>.Fail(result.StatusCode, result.Error ?? "Could not load commission rules.");
        }

        return ApiResult<CommissionsVm>.Ok(CommissionsMapper.ToVm(result.Data, tier, currency, includeInactive));
    }

    public Task<ApiResult> CreateAsync(CreateCommissionRuleRequest request, CancellationToken ct = default)
        => Normalize(_api.CreateAsync(request, ct), "Could not create the commission rule.");

    public Task<ApiResult> UpdateAsync(Guid id, UpdateCommissionRuleRequest request, CancellationToken ct = default)
        => Normalize(_api.UpdateAsync(id, request, ct), "Could not update the commission rule.");

    public Task<ApiResult> DeleteAsync(Guid id, string? reason, CancellationToken ct = default)
        => Normalize(_api.DeleteAsync(id, reason, ct), "Could not delete the commission rule.");

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
            return ApiResult.Fail(404, "Commission rule not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "A commission rule for this tier and currency range already exists. Please reload and adjust.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
