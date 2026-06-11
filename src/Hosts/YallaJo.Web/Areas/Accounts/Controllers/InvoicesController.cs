using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// Phase 3 (Accounts master plan): the standalone invoices page moved into the Billing hub
/// (/accounts/payments?tab=invoices). Index now permanently redirects there; the PDF
/// Download action keeps its route and permission gate unchanged.
/// </summary>
[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.Invoice.Read)]
public sealed class InvoicesController : BaseController
{
    private readonly InvoicesFacade _invoices;

    public InvoicesController(InvoicesFacade invoices)
    {
        _invoices = invoices;
    }

    [HttpGet("accounts/invoices")]
    public IActionResult Index()
        => RedirectPermanent(Url.Action("Index", "Payments", new { area = "Accounts", tab = "invoices" })!);

    [HttpGet("accounts/invoices/{id:guid}/download")]
    [RequirePermission(WebPermission.Invoice.Download)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await _invoices.DownloadAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Could not download the invoice.");
            return RedirectToAction("Index", "Payments", new { area = "Accounts", tab = "invoices" });
        }

        var file = result.Data;
        return File(file.Content, file.ContentType, file.FileName);
    }
}
