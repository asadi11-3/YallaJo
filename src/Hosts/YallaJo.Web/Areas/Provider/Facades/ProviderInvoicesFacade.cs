using Microsoft.Extensions.Logging;
using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Invoices;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class ProviderInvoicesFacade
{
    private const int PageSize = 50;

    private readonly ProviderInvoicesApiClient _api;
    private readonly ILogger<ProviderInvoicesFacade> _logger;

    public ProviderInvoicesFacade(ProviderInvoicesApiClient api, ILogger<ProviderInvoicesFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<ProviderInvoicesVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMyInvoicesAsync(pageSize: PageSize, ct: ct);

        if (result.RequireSignOut)
        {
            return ApiResult<ProviderInvoicesVm>.ForceSignOut();
        }

        // 403 means the caller's provider could not be resolved (e.g. their token
        // predates the provider_id claim). Surface an actionable message rather than a raw error.
        if (result.IsForbidden)
        {
            return ApiResult<ProviderInvoicesVm>.Fail(403,
                "We couldn't find your provider account on this session. Please sign out and sign in again, then try once more.");
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<ProviderInvoicesVm>.Fail(result.StatusCode, result.Error ?? "Could not load your invoices.");
        }

        var rows = result.Data.Items
            .OrderByDescending(i => i.IssuedAt)
            .Select(ToRow)
            .ToList();

        return ApiResult<ProviderInvoicesVm>.Ok(new ProviderInvoicesVm { Invoices = rows });
    }

    public async Task<ApiResult<ApiFile>> DownloadAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _api.DownloadAsync(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download provider invoice {InvoiceId}", id);
            return ApiResult<ApiFile>.Fail(500, "Could not download the invoice.");
        }
    }

    private static ProviderInvoiceRowVm ToRow(ProviderInvoiceResponse i) => new(
        i.Id,
        i.InvoiceNumber,
        i.Status,
        i.Currency,
        i.AmountTotal,
        i.IssuedAt,
        i.BuyerName,
        i.PdfAvailable,
        i.Items.Count);
}
