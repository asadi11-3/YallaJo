using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Disputes;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class DisputesFacade(DisputesApiClient api)
{
    private readonly DisputesApiClient _api = api;

    public async Task<ApiResult<DisputesVm>> GetIndexAsync(string? status, CancellationToken ct)
    {
        var result = await _api.GetOpenAsync(ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<DisputesVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<DisputesVm>.Fail(result.StatusCode, result.Error ?? "Could not load open disputes.");
        }

        return ApiResult<DisputesVm>.Ok(DisputesMapper.ToVm(result.Data, status));
    }

    public Task<ApiResult> ReviewAsync(Guid id, CancellationToken ct) =>
        Normalize(_api.ReviewAsync(id, ct), "Could not mark the dispute under review.");

    public Task<ApiResult> ResolveAsync(Guid id, string resolution, string? notes, CancellationToken ct) =>
        Normalize(_api.ResolveAsync(id, resolution, notes, ct), "Could not resolve the dispute.");

    public Task<ApiResult> EscalateAsync(Guid id, string reason, CancellationToken ct) =>
        Normalize(_api.EscalateAsync(id, reason, ct), "Could not escalate the dispute.");

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
            return ApiResult.Fail(404, "Dispute not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the dispute's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
