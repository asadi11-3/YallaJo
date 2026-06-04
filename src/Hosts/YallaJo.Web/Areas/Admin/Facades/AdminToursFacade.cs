using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class AdminToursFacade
{
    private readonly AdminToursApiClient _api;

    public AdminToursFacade(AdminToursApiClient api) => _api = api;

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

        return ApiResult<AdminTourDetailsVm>.Ok(AdminToursMapper.ToDetailsVm(result.Data));
    }

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.ApproveAsync(id, rv, ct), "Could not approve the tour.", ct);

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.RejectAsync(id, rv, reason, ct), "Could not reject the tour.", ct);

    public Task<ApiResult> SuspendAsync(Guid id, string reason, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.SuspendAsync(id, rv, reason, ct), "Could not suspend the tour.", ct);

    public Task<ApiResult> ReinstateAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.ReinstateAsync(id, rv, ct), "Could not reinstate the tour.", ct);


    private async Task<ApiResult> WithRowVersion(
        Guid id, Func<byte[], Task<ApiResult>> mutate, string fallback, CancellationToken ct)
    {
        var detail = await _api.GetByIdAsync(id, ct);
        if (detail.IsUnauthorized) return ApiResult.ForceSignOut();
        if (detail.IsNotFound) return ApiResult.Fail(404, "Tour not found.");
        if (!detail.IsSuccess || detail.Data is null)
            return ApiResult.Fail(detail.StatusCode, detail.Error ?? fallback);

        if (detail.Data.RowVersion is not { Length: > 0 })
            return ApiResult.Fail(409, "This tour is missing concurrency data. Please reload and try again.");

        return await Normalize(mutate(detail.Data.RowVersion), fallback);
    }

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess) return ApiResult.Ok();
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
