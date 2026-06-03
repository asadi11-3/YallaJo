using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Earnings;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class EarningsFacade
{
    private readonly EarningsApiClient _api;

    public EarningsFacade(EarningsApiClient api) => _api = api;

    public async Task<ApiResult<EarningsVm>> GetEarningsAsync(CancellationToken ct = default)
    {
        // Summary first: a 401 here means the session is gone, so force sign-out.
        var summary = await _api.GetSummaryAsync(ct);
        if (summary.IsUnauthorized)
            return ApiResult<EarningsVm>.ForceSignOut();

        var earningsTask = SafeListAsync(() => _api.GetEarningsAsync(ct));
        var payoutsTask = SafePayoutsAsync(ct);
        var disputesTask = SafeListAsync(() => _api.GetDisputesAsync(ct));

        await Task.WhenAll(earningsTask, payoutsTask, disputesTask);

        var summaryData = summary is { IsSuccess: true, Data: not null } ? summary.Data : null;

        var vm = new EarningsVm
        {
            GrossTotal = summaryData?.GrossTotal ?? 0m,
            NetEstimateTotal = summaryData?.NetEstimateTotal ?? 0m,
            PaymentCount = summaryData?.PaymentCount ?? 0,
            Currency = summaryData?.Currency ?? string.Empty,
            Earnings = earningsTask.Result
                .OrderByDescending(e => e.PaidAt ?? DateTime.MinValue)
                .Select(e => new EarningRowVm
                {
                    BookingId = e.BookingId,
                    GrossAmount = e.GrossAmount,
                    NetEstimate = e.NetEstimate,
                    Currency = e.Currency,
                    PaidAt = e.PaidAt
                })
                .ToList(),
            Payouts = payoutsTask.Result
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PayoutRowVm
                {
                    Id = p.Id,
                    Status = p.Status,
                    Currency = p.Currency,
                    BatchPeriodStart = p.BatchPeriodStart,
                    BatchPeriodEnd = p.BatchPeriodEnd,
                    GrossAmount = p.GrossAmount,
                    CommissionAmount = p.CommissionAmount,
                    NetAmount = p.NetAmount,
                    ItemCount = p.ItemCount,
                    CompletedAt = p.CompletedAt,
                    CreatedAt = p.CreatedAt
                })
                .ToList(),
            Disputes = disputesTask.Result
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DisputeRowVm
                {
                    Reason = d.Reason,
                    Description = d.Description,
                    Status = d.Status,
                    Resolution = d.Resolution,
                    CreatedAt = d.CreatedAt,
                    ResolvedAt = d.ResolvedAt
                })
                .ToList()
        };

        return ApiResult<EarningsVm>.Ok(vm);
    }

    private static async Task<List<T>> SafeListAsync<T>(Func<Task<ApiResult<List<T>>>> call)
    {
        try
        {
            var result = await call();
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch
        {
            return [];
        }
    }

    private async Task<List<PayoutResponse>> SafePayoutsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPayoutsAsync(ct);
            return result is { IsSuccess: true, Data.Items: not null }
                ? result.Data.Items.ToList()
                : [];
        }
        catch
        {
            return [];
        }
    }
}
