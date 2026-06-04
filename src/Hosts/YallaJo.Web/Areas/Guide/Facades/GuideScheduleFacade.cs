using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideScheduleFacade
{
    private readonly GuideApiClient _api;

    public GuideScheduleFacade(GuideApiClient api) => _api = api;

    public async Task<ApiResult<GuideScheduleVm>> GetAsync(CancellationToken ct = default)
    {
        var profile = await _api.GetMyProfileAsync(ct);
        if (profile.IsUnauthorized)
        {
            return ApiResult<GuideScheduleVm>.ForceSignOut();
        }

        if (profile is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<GuideScheduleVm>.Ok(new GuideScheduleVm { IsGuide = false });
        }

        var vm = new GuideScheduleVm { IsGuide = true };

        try
        {
            var blocks = await _api.GetAvailabilityBlocksAsync(ct);
            if (blocks is { IsSuccess: true, Data: not null })
            {
                vm.Blocks = blocks.Data
                    .OrderBy(b => b.StartDate)
                    .Select(b => new AvailabilityBlockRowVm
                    {
                        Id = b.Id,
                        StartDate = b.StartDate,
                        EndDate = b.EndDate,
                        Reason = b.Reason,
                        CreatedAt = b.CreatedAt,
                    })
                    .ToList();
            }
        }
        catch
        {
            // Tolerate hydration failure.
        }

        return ApiResult<GuideScheduleVm>.Ok(vm);
    }

    public Task<ApiResult> AddAsync(AddBlockVm form, CancellationToken ct = default) =>
        NormalizeAsync(
            () => _api.AddAvailabilityBlockAsync(
                new CreateGuideAvailabilityBlockRequest(form.StartDate, form.EndDate, string.IsNullOrWhiteSpace(form.Reason) ? null : form.Reason.Trim()),
                ct),
            "Could not add the unavailable dates.");

    public Task<ApiResult> DeleteAsync(Guid blockId, CancellationToken ct = default) =>
        NormalizeAsync(() => _api.DeleteAvailabilityBlockAsync(blockId, ct), "Could not remove the block.");

    private static async Task<ApiResult> NormalizeAsync(Func<Task<ApiResult>> call, string fallback)
    {
        ApiResult result;
        try
        {
            result = await call();
        }
        catch
        {
            return ApiResult.Fail(500, fallback);
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsSuccess)
        {
            return ApiResult.Ok(result.StatusCode);
        }

        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
