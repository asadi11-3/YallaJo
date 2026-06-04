using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.GuideApplications;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class GuideApplicationsFacade
{
    private const int PageSize = 20;

    private readonly GuideApplicationsApiClient _api;

    public GuideApplicationsFacade(GuideApplicationsApiClient api) => _api = api;

    public async Task<ApiResult<GuideApplicationsVm>> GetIndexAsync(
        Guid? tourId,
        string? status,
        int page,
        CancellationToken ct)
    {
        if (tourId is not { } id || id == Guid.Empty)
        {
            return ApiResult<GuideApplicationsVm>.Ok(new GuideApplicationsVm
            {
                StatusFilter = status,
                Page = page,
                PageSize = PageSize,
            });
        }

        var result = await _api.GetApplicationsAsync(id, status, page, PageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<GuideApplicationsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<GuideApplicationsVm>.Fail(
                result.StatusCode,
                result.Error ?? "Could not load guide applications.");
        }

        return ApiResult<GuideApplicationsVm>.Ok(
            GuideApplicationsMapper.ToVm(result.Data, id, status, page, PageSize));
    }

    public Task<ApiResult> ApproveAsync(Guid tourId, Guid applicationId, CancellationToken ct) =>
        Normalize(_api.ApproveAsync(tourId, applicationId, ct), "Could not approve the guide application.");

    public Task<ApiResult> RejectAsync(Guid tourId, Guid applicationId, string reason, CancellationToken ct) =>
        Normalize(_api.RejectAsync(tourId, applicationId, reason, ct), "Could not reject the guide application.");

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
            return ApiResult.Fail(404, "Guide application not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action is not allowed in the application's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
