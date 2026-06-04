using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Payouts;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class PayoutsFacade
{
    private const int DefaultPageSize = 25;
    private readonly PayoutsApiClient _api;

    public PayoutsFacade(PayoutsApiClient api) => _api = api;

    public async Task<ApiResult<PayoutsVm>> GetIndexAsync(Guid? cursor, int pageSize, CancellationToken ct)
    {
        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = DefaultPageSize;
        }

        var result = await _api.GetPendingAsync(cursor, pageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<PayoutsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<PayoutsVm>.Fail(result.StatusCode, result.Error ?? "Could not load pending payouts.");
        }

        return ApiResult<PayoutsVm>.Ok(PayoutsMapper.ToVm(result.Data));
    }

    public Task<ApiResult> TriggerAsync(CancellationToken ct)
        => Normalize(_api.TriggerAsync(ct), "Could not trigger the payout batch.");

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct)
        => Normalize(_api.ApproveAsync(id, ct), "Could not approve the payout.");

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
            return ApiResult.Fail(404, "Payout not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the payout's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
