using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Disputes;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// §3.8 Disputes — the customer's standalone disputes list + open-dispute form, served
/// at <c>/accounts/disputes</c>. Reads <c>GET /disputes/my</c> and opens via
/// <c>POST /disputes</c> (Finance module). Owner scoping is enforced by the API.
/// <para>
/// Cache: authenticated ⇒ NoStore by the global base policy. Perm: <c>[Authorize]</c>
/// plus <c>Permission.Refund.{Read,Create}</c> which mirror the backend
/// <c>FinanceFeatures.Refund</c> Read/Create requirements on these two endpoints.
/// Every write carries anti-forgery and follows PRG.
/// </para>
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class DisputesController : BaseController
{
    private readonly DisputesFacade _disputes;
    private readonly ProfileFacade _profile;

    public DisputesController(DisputesFacade disputes, ProfileFacade profile)
    {
        _disputes = disputes;
        _profile = profile;
    }

    [HttpGet("accounts/disputes")]
    [RequirePermission(WebPermission.Refund.Read)]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        ViewData["AccountNav"] = "Disputes";
        await PopulateSidebarAsync(ct);

        var result = await _disputes.GetAsync(page, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new DisputesVm { PageNumber = page < 1 ? 1 : page });
        }

        return View(result.Data);
    }

    [HttpPost("accounts/disputes")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Refund.Create)]
    public async Task<IActionResult> Open(OpenDisputeFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please correct the highlighted fields and try again.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _disputes.OpenAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your dispute has been submitted. Our team will review it shortly.");
        else
            SetError(result.Error ?? "Could not open the dispute.");

        return RedirectToAction(nameof(Index));
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
