using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Earnings;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideEarningsFacade
{
    private readonly EarningsApiClient _api;
    private readonly ILogger<GuideEarningsFacade> _logger;

    public GuideEarningsFacade(EarningsApiClient api, ILogger<GuideEarningsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<EarningsVm>> GetAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var summaryResult = await _api.GetSummaryAsync(ct);
        if (summaryResult.RequireSignOut)
        {
            return ApiResult<EarningsVm>.ForceSignOut();
        }

        if (!summaryResult.IsSuccess || summaryResult.Data is null)
        {
            return ApiResult<EarningsVm>.Fail(summaryResult.StatusCode, summaryResult.Error);
        }

        var summary = summaryResult.Data;

        var byTourTask = SafeByTourAsync(ct);
        var historyTask = SafeHistoryAsync(page, pageSize, ct);
        await Task.WhenAll(byTourTask, historyTask);

        var byTour = byTourTask.Result;
        var history = historyTask.Result;

        var vm = new EarningsVm
        {
            TotalEarned = summary.TotalEarned,
            ThisMonth = summary.ThisMonth,
            PendingPayout = summary.PendingPayout,
            CommissionDeducted = summary.CommissionDeducted,
            NetEarnings = summary.NetEarnings,
            Currency = string.IsNullOrWhiteSpace(summary.Currency) ? "JOD" : summary.Currency,
            ByTour = byTour
                .Select(b => new EarningByTourRowVm(
                    b.TourId, b.TourName, b.BookingCount, b.GrossAmount, b.CommissionAmount, b.NetAmount, b.Currency))
                .ToList(),
            History = history.Items
                .Select(h => new EarningHistoryRowVm(
                    h.EarningId, h.BookingId, h.EarnedDate, h.GrossAmount, h.CommissionAmount, h.NetAmount, h.Currency, h.Status))
                .ToList(),
            Page = history.PageNumber > 0 ? history.PageNumber : page,
            PageSize = history.PageSize > 0 ? history.PageSize : pageSize,
            TotalCount = history.TotalCount,
        };

        return ApiResult<EarningsVm>.Ok(vm);
    }

    private async Task<List<GuideEarningByTourResponse>> SafeByTourAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetByTourAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load guide earnings by-tour breakdown.");
            return [];
        }
    }

    private async Task<PaginatedResponse<GuideEarningHistoryItemResponse>> SafeHistoryAsync(
        int page,
        int pageSize,
        CancellationToken ct)
    {
        try
        {
            var result = await _api.GetHistoryAsync(page, pageSize, ct);
            return result is { IsSuccess: true, Data: not null }
                ? result.Data
                : new PaginatedResponse<GuideEarningHistoryItemResponse> { PageNumber = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load guide earnings history.");
            return new PaginatedResponse<GuideEarningHistoryItemResponse> { PageNumber = page, PageSize = pageSize };
        }
    }
}
