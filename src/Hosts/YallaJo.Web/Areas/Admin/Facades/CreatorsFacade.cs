using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Creators;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class CreatorsFacade
{
    private readonly CreatorsApiClient _api;

    public CreatorsFacade(CreatorsApiClient api) => _api = api;

    public async Task<ApiResult<CreatorsVm>> GetIndexAsync(
        string? status, Guid? id, int page, CancellationToken ct)
    {
        var pageSize = 20;
        var list = await _api.GetApplicationsAsync(status, page, pageSize, ct);
        if (list.IsUnauthorized)
        {
            return ApiResult<CreatorsVm>.ForceSignOut();
        }

        if (list is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<CreatorsVm>.Fail(list.StatusCode, list.Error ?? "Could not load creator applications.");
        }

        CreatorApplicationDetailResponse? detail = null;
        if (id is { } applicationId && applicationId != Guid.Empty)
        {
            var d = await _api.GetApplicationAsync(applicationId, ct);
            if (d.IsUnauthorized)
            {
                return ApiResult<CreatorsVm>.ForceSignOut();
            }

            if (d is { IsSuccess: true, Data: not null })
            {
                detail = d.Data;
            }
        }

        return ApiResult<CreatorsVm>.Ok(CreatorsMapper.ToVm(list.Data, detail, status));
    }

    public Task<ApiResult> ApproveAsync(Guid id, string displayName, string? avatarUrl, CancellationToken ct)
        => Normalize(_api.ApproveAsync(id, displayName, avatarUrl, ct), "Could not approve the creator application.");

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct)
        => Normalize(_api.RejectAsync(id, reason, ct), "Could not reject the creator application.");

    public Task<ApiResult> RequestMoreInfoAsync(Guid id, string adminNote, CancellationToken ct)
        => Normalize(_api.RequestMoreInfoAsync(id, adminNote, ct), "Could not request more information.");

    public Task<ApiResult> SuspendAsync(Guid profileId, string reason, CancellationToken ct)
        => Normalize(_api.SuspendAsync(profileId, reason, ct), "Could not suspend the creator profile.");

    public Task<ApiResult> ReinstateAsync(Guid profileId, CancellationToken ct)
        => Normalize(_api.ReinstateAsync(profileId, ct), "Could not reinstate the creator profile.");

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
            return ApiResult.Fail(404, "Creator application or profile not found.");
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
