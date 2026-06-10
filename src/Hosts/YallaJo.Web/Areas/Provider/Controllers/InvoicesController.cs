using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

/// <summary>
/// Provider self-view of invoices issued for their bookings. Seller-scoped on the
/// backend via the server-issued <c>provider_id</c> claim.
/// The standalone invoices page was retired into the Finance hub (Invoices tab);
/// <see cref="Index"/> remains only as a permanent redirect so old links keep working.
/// </summary>
[Area("Provider")]
[Authorize]
[RequirePermission(WebPermission.Invoice.Read)]
public sealed class InvoicesController : BaseController
{
    private readonly ProviderInvoicesFacade _invoices;

    public InvoicesController(ProviderInvoicesFacade invoices) => _invoices = invoices;

    /// <summary>Retired page — 301 into the Finance hub's Invoices tab.</summary>
    [HttpGet("provider/invoices")]
    public IActionResult Index() => RedirectPermanent("/provider/finance#invoices");

    [HttpGet("provider/invoices/{id:guid}/download")]
    [RequirePermission(WebPermission.Invoice.Download)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await _invoices.DownloadAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Could not download the invoice.");
            return RedirectToAction("Index", "Finance");
        }

        var file = result.Data;
        return File(file.Content, file.ContentType, file.FileName);
    }
}
