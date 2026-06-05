using Microsoft.Extensions.Logging;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Invoices;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class InvoicesFacade
{
    private const int PageSize = 50;

    private readonly InvoicesApiClient _api;
    private readonly ILogger<InvoicesFacade> _logger;

    public InvoicesFacade(InvoicesApiClient api, ILogger<InvoicesFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<InvoicesVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMyInvoicesAsync(pageSize: PageSize, ct: ct);

        if (result.RequireSignOut)
        {
            return ApiResult<InvoicesVm>.ForceSignOut();
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<InvoicesVm>.Fail(result.StatusCode, result.Error);
        }

        var rows = result.Data.Items
            .OrderByDescending(i => i.IssuedAt)
            .Select(ToRow)
            .ToList();

        return ApiResult<InvoicesVm>.Ok(new InvoicesVm { Invoices = rows });
    }

    public async Task<ApiResult<ApiFile>> DownloadAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _api.DownloadAsync(id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download invoice {InvoiceId}", id);
            return ApiResult<ApiFile>.Fail(500, "Could not download the invoice.");
        }
    }

    private static InvoiceRowVm ToRow(InvoiceResponse i) => new(
        i.Id,
        i.InvoiceNumber,
        i.Status,
        i.Currency,
        i.AmountTotal,
        i.IssuedAt,
        i.SellerName,
        i.PdfAvailable,
        i.Items.Count);
}
