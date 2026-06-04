using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Businesses;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class BusinessesFacade
{
    private const int PageSize = 20;
    private readonly BusinessesApiClient _api;

    public BusinessesFacade(BusinessesApiClient api) => _api = api;

    public async Task<ApiResult<BusinessesVm>> GetIndexAsync(
        Guid? placeId, string? status, int page, CancellationToken ct = default)
    {
        if (placeId is not { } id || id == Guid.Empty)
        {
            return ApiResult<BusinessesVm>.Ok(new BusinessesVm
            {
                StatusFilter = status,
                PageNumber = page,
                PageSize = PageSize,
            });
        }

        var result = await _api.GetByPlaceAsync(id, page, PageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<BusinessesVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<BusinessesVm>.Fail(
                result.StatusCode, result.Error ?? "Could not load businesses.");
        }

        return ApiResult<BusinessesVm>.Ok(BusinessesMapper.ToVm(result.Data, placeId, status));
    }

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default) =>
        Normalize(_api.ApproveAsync(id, ct), "Could not approve the business.");

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct = default) =>
        Normalize(_api.RejectAsync(id, reason, ct), "Could not reject the business.");

    public Task<ApiResult> RequestMoreDocsAsync(Guid id, string reason, CancellationToken ct = default) =>
        Normalize(_api.RequestMoreDocsAsync(id, reason, ct), "Could not request more documents.");

    public Task<ApiResult> SuspendAsync(Guid id, string reason, CancellationToken ct = default) =>
        Normalize(_api.SuspendAsync(id, reason, ct), "Could not suspend the business.");

    public Task<ApiResult> ReinstateAsync(Guid id, CancellationToken ct = default) =>
        Normalize(_api.ReinstateAsync(id, ct), "Could not reinstate the business.");

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default) =>
        Normalize(_api.DeleteAsync(id, ct), "Could not delete the business.");

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
            return ApiResult.Fail(404, "Business not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409,
                "This action is not allowed in the business's current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
