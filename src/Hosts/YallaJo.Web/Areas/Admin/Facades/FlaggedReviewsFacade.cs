using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.FlaggedReviews;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class FlaggedReviewsFacade
{
    private const int DefaultPageSize = 20;
    private readonly FlaggedReviewsApiClient _api;

    public FlaggedReviewsFacade(FlaggedReviewsApiClient api) => _api = api;

    public async Task<ApiResult<FlaggedReviewsVm>> GetIndexAsync(Guid? afterCursor, int pageSize, CancellationToken ct)
    {
        if (pageSize < 1 || pageSize > 100)
        {
            pageSize = DefaultPageSize;
        }

        var result = await _api.GetFlaggedAsync(afterCursor, pageSize, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<FlaggedReviewsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<FlaggedReviewsVm>.Fail(result.StatusCode, result.Error ?? "Could not load flagged reviews.");
        }

        return ApiResult<FlaggedReviewsVm>.Ok(FlaggedReviewsMapper.ToVm(result.Data));
    }

    public Task<ApiResult> ApproveAsync(Guid id, string? rowVersion, string? notes, CancellationToken ct)
        => Normalize(_api.ApproveAsync(id, rowVersion, notes, ct), "Could not approve the review.");

    public Task<ApiResult> RemoveAsync(Guid id, string? rowVersion, string? notes, CancellationToken ct)
        => Normalize(_api.RemoveAsync(id, rowVersion, notes, ct), "Could not remove the review.");

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
            return ApiResult.Fail(404, "Review not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This review was modified by someone else. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
