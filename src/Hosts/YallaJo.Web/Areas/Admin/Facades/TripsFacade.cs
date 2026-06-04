using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Trips;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// Orchestrates the admin Tour-approvals screen: loads the (approved) tours list plus, when a
/// specific tour id is looked up, its privileged detail (which carries the RowVersion concurrency
/// token needed for moderation). Moderation writes are normalized to a uniform <see cref="ApiResult"/>.
/// </summary>
public sealed class TripsFacade
{
    private readonly TripsApiClient _api;

    public TripsFacade(TripsApiClient api) => _api = api;

    public async Task<ApiResult<TripsVm>> GetIndexAsync(Guid? id, int page, CancellationToken ct)
    {
        var list = await _api.GetToursAsync(page, 20, ct);
        if (list.IsUnauthorized) return ApiResult<TripsVm>.ForceSignOut();
        if (!list.IsSuccess || list.Data is null)
            return ApiResult<TripsVm>.Fail(list.StatusCode, list.Error ?? "Could not load tours.");

        TourDetailResponse? detail = null;
        if (id is { } tid && tid != Guid.Empty)
        {
            var d = await _api.GetTourAsync(tid, ct);
            if (d.IsUnauthorized) return ApiResult<TripsVm>.ForceSignOut();
            if (d.IsSuccess) detail = d.Data;
        }

        return ApiResult<TripsVm>.Ok(TripsMapper.ToVm(list.Data, detail));
    }

    public async Task<ApiResult> ApproveAsync(Guid id, string rowVersionBase64, CancellationToken ct)
    {
        if (!TryFromBase64(rowVersionBase64, out var rv)) return InvalidToken();
        return await Normalize(_api.ApproveAsync(id, rv, ct), "Could not approve the tour.");
    }

    public async Task<ApiResult> ReinstateAsync(Guid id, string rowVersionBase64, CancellationToken ct)
    {
        if (!TryFromBase64(rowVersionBase64, out var rv)) return InvalidToken();
        return await Normalize(_api.ReinstateAsync(id, rv, ct), "Could not reinstate the tour.");
    }

    public async Task<ApiResult> RejectAsync(Guid id, string rowVersionBase64, string reason, CancellationToken ct)
    {
        if (!TryFromBase64(rowVersionBase64, out var rv)) return InvalidToken();
        return await Normalize(_api.RejectAsync(id, rv, reason, ct), "Could not reject the tour.");
    }

    public async Task<ApiResult> SuspendAsync(Guid id, string rowVersionBase64, string reason, CancellationToken ct)
    {
        if (!TryFromBase64(rowVersionBase64, out var rv)) return InvalidToken();
        return await Normalize(_api.SuspendAsync(id, rv, reason, ct), "Could not suspend the tour.");
    }

    private static ApiResult InvalidToken()
        => ApiResult.Fail(400, "Invalid concurrency token. Please reload and try again.");

    private static bool TryFromBase64(string? value, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "Tour not found.");
        if (result.IsConflict)
            return ApiResult.Fail(409, "This action is not allowed in the tour's current state. Please reload and try again.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
