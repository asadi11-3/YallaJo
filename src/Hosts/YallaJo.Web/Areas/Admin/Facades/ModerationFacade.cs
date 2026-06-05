using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Moderation;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class ModerationFacade
{
    private const int DefaultPageSize = 20;
    private readonly ModerationApiClient _api;

    public ModerationFacade(ModerationApiClient api) => _api = api;

    public async Task<ApiResult<ModerationVm>> GetIndexAsync(Guid? afterCursor, int pageSize, CancellationToken ct)
    {
        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = DefaultPageSize;
        }

        var result = await _api.GetLogsAsync(afterCursor, pageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<ModerationVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<ModerationVm>.Fail(result.StatusCode, result.Error ?? "Could not load moderation log.");
        }

        return ApiResult<ModerationVm>.Ok(ModerationMapper.ToVm(result.Data));
    }

    public Task<ApiResult> WarnAsync(WarnUserFormVm form, CancellationToken ct)
    {
        var request = new WarnUserApiRequest(form.UserId, form.EntityType, form.EntityId, form.Reason.Trim());
        return Normalize(_api.WarnAsync(request, ct), "Could not issue the warning.");
    }

    public Task<ApiResult> BanAsync(BanUserFormVm form, CancellationToken ct)
    {
        var request = new BanUserApiRequest(form.UserId, form.EntityType, form.EntityId, form.Reason.Trim(), form.ExpiresAt);
        return Normalize(_api.BanAsync(request, ct), "Could not issue the ban.");
    }

    public Task<ApiResult> UnbanAsync(Guid userId, CancellationToken ct)
        => Normalize(_api.UnbanAsync(userId, ct), "Could not lift the ban.");

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
            return ApiResult.Fail(404, "The target was not found.");
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
