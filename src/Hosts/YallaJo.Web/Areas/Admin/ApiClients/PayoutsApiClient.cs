using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Payouts;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class PayoutsApiClient
{
    private const string Base = "/api/v1/payouts";
    private readonly IApiClient _api;

    public PayoutsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<PayoutPageResponse>> GetPendingAsync(Guid? cursor, int pageSize, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (cursor is { } c && c != Guid.Empty)
        {
            query["cursor"] = c.ToString("D");
        }

        var url = QueryHelpers.AddQueryString($"{Base}/admin/pending", query);
        return _api.GetAsync<PayoutPageResponse>(url, ct);
    }

    public Task<ApiResult> TriggerAsync(CancellationToken ct)
        => _api.PostAsync($"{Base}/admin/trigger", null, ct);

    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct)
        => _api.PostAsync($"{Base}/{id:D}/approve", null, ct);
}
