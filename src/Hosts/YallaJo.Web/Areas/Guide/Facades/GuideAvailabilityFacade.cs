using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Availability;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideAvailabilityFacade
{
    private readonly AvailabilityApiClient _api;
    private readonly ILogger<GuideAvailabilityFacade> _logger;

    public GuideAvailabilityFacade(AvailabilityApiClient api, ILogger<GuideAvailabilityFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<AvailabilityVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetBlocksAsync(ct);
        if (result.RequireSignOut)
        {
            return ApiResult<AvailabilityVm>.ForceSignOut();
        }

        if (!result.IsSuccess)
        {
            return ApiResult<AvailabilityVm>.Fail(result.StatusCode, result.Error);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var blocks = (result.Data ?? [])
            .OrderByDescending(b => b.StartDate)
            .Select(b => new AvailabilityBlockRowVm(
                b.Id,
                b.StartDate,
                b.EndDate,
                b.Reason,
                b.CreatedAt,
                b.EndDate >= today))
            .ToList();

        return ApiResult<AvailabilityVm>.Ok(new AvailabilityVm { Blocks = blocks });
    }

    public async Task<ApiResult> AddBlockAsync(AddAvailabilityBlockFormVm form, CancellationToken ct = default)
    {
        var reason = string.IsNullOrWhiteSpace(form.Reason) ? null : form.Reason.Trim();
        var request = new CreateGuideAvailabilityBlockRequest(form.StartDate, form.EndDate, reason);
        return await _api.AddBlockAsync(request, ct);
    }

    public async Task<ApiResult> DeleteBlockAsync(Guid blockId, CancellationToken ct = default) =>
        await _api.DeleteBlockAsync(blockId, ct);
}
