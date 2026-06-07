using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.Invoices;
using YallaJo.Web.Areas.Provider.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

/// <summary>
/// Provider self-view of invoices issued for their bookings. Seller-scoped on the
/// backend via the server-issued <c>provider_id</c> claim.
/// </summary>
[Area("Provider")]
[Authorize]
[RequirePermission(WebPermission.Invoice.Read)]
public sealed class InvoicesController : BaseController
{
    private readonly ProviderInvoicesFacade _invoices;

    public InvoicesController(ProviderInvoicesFacade invoices) => _invoices = invoices;

    [HttpGet("provider/invoices")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        SetSidebar();

        var result = await _invoices.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ProviderInvoicesVm());
        }

        return View(result.Data);
    }

    [HttpGet("provider/invoices/{id:guid}/download")]
    [RequirePermission(WebPermission.Invoice.Download)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await _invoices.DownloadAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error ?? "Could not download the invoice.");
            return RedirectToAction(nameof(Index));
        }

        var file = result.Data;
        return File(file.Content, file.ContentType, file.FileName);
    }

    private void SetSidebar()
    {
        ViewData["ProviderNav"] = "Invoices";
        ViewBag.Sidebar = new ProviderSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Provider",
        };
    }
}
