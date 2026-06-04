using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Payments;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class PaymentsFacade
{
    private readonly PaymentsApiClient _api;

    public PaymentsFacade(PaymentsApiClient api) => _api = api;

    public async Task<ApiResult<PaymentsVm>> GetEarningsAsync(PaymentsFilterRequest request, CancellationToken ct = default)
    {
        var pageSize = request.PageSize is < 1 or > 200 ? 25 : request.PageSize;

        var dashboard = await _api.GetDashboardAsync(ct);
        if (dashboard.IsUnauthorized)
        {
            return ApiResult<PaymentsVm>.ForceSignOut();
        }

        if (dashboard is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<PaymentsVm>.Fail(dashboard.StatusCode, dashboard.Error ?? "Could not load the finance dashboard.");
        }

        var payments = await _api.GetAdminPaymentsAsync(request.Status, request.Type, request.Cursor, pageSize, ct);
        if (payments.IsUnauthorized)
        {
            return ApiResult<PaymentsVm>.ForceSignOut();
        }

        if (payments is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<PaymentsVm>.Fail(payments.StatusCode, payments.Error ?? "Could not load payments.");
        }

        var vm = PaymentsMapper.ToVm(dashboard.Data, payments.Data, request.Status, request.Type);
        return ApiResult<PaymentsVm>.Ok(vm);
    }
}
