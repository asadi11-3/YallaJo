using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Invoices;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.Invoice.Read)]
public sealed class InvoicesController : BaseController
{
    private readonly InvoicesFacade _invoices;
    private readonly ProfileFacade _profile;

    public InvoicesController(InvoicesFacade invoices, ProfileFacade profile)
    {
        _invoices = invoices;
        _profile = profile;
    }

    [HttpGet("accounts/invoices")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "Invoices";
        await PopulateSidebarAsync(ct);

        var result = await _invoices.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new InvoicesVm());
        }

        return View(result.Data);
    }

    [HttpGet("accounts/invoices/{id:guid}/download")]
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

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
