using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Provider.Models.Invoices;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

/// <summary>
/// Typed access to the seller-scoped Finance invoice endpoints for the provider's
/// "Invoices" page. The backend resolves the provider from the server-issued
/// <c>provider_id</c> claim — nothing here is trusted from the client.
/// </summary>
public sealed class ProviderInvoicesApiClient
{
    private const string Base = "/api/v1/invoices";

    private readonly IApiClient _api;

    public ProviderInvoicesApiClient(IApiClient api) => _api = api;

    // GET /api/v1/invoices/provider/my-invoices
    public Task<ApiResult<ProviderInvoicePageResponse>> GetMyInvoicesAsync(
        Guid? cursor = null,
        int pageSize = 50,
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

        var url = QueryHelpers.AddQueryString($"{Base}/provider/my-invoices", query);
        return _api.GetAsync<ProviderInvoicePageResponse>(url, ct);
    }

    // GET /api/v1/invoices/{id}/download — reuses the existing invoice PDF download.
    public Task<ApiResult<ApiFile>> DownloadAsync(Guid id, CancellationToken ct = default)
        => _api.GetFileAsync($"{Base}/{id}/download", ct);
}
