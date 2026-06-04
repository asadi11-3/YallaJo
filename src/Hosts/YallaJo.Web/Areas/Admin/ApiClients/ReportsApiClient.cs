using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.Reports;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class ReportsApiClient
{
    private const string Base = "/api/v1/reports";
    private readonly IApiClient _api;

    public ReportsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ReportPageResponse>> GetAdminReportsAsync(Guid? afterCursor, int pageSize, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        };
        if (afterCursor is { } cursor && cursor != Guid.Empty)
        {
            query["afterCursor"] = cursor.ToString("D");
        }

        var url = QueryHelpers.AddQueryString($"{Base}/admin", query);
        return _api.GetAsync<ReportPageResponse>(url, ct);
    }

    public Task<ApiResult> ResolveAsync(Guid id, string action, string? notes, CancellationToken ct)
        => _api.PostAsync($"{Base}/admin/{id:D}/resolve", new { action, notes }, ct);
}
