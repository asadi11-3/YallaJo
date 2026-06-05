using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Accounts.Models.Invoices;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class InvoicesApiClient
{
    private const string Base = "/api/v1/invoices";

    private readonly IApiClient _api;

    public InvoicesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<InvoicePageResponse>> GetMyInvoicesAsync(
        Guid? cursor = null,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString(),
        };

        if (cursor is { } c)
        {
            query["cursor"] = c.ToString();
        }

        var url = QueryHelpers.AddQueryString($"{Base}/my-invoices", query);
        return _api.GetAsync<InvoicePageResponse>(url, ct);
    }

    public Task<ApiResult<ApiFile>> DownloadAsync(Guid id, CancellationToken ct = default) =>
        _api.GetFileAsync($"{Base}/{id}/download", ct);
}
