using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Disputes;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// §3.8 Disputes. Phase 3 (Accounts master plan): the standalone disputes page moved into
/// the Billing hub (/accounts/payments?tab=disputes). Index now permanently redirects
/// there; Open keeps its route, anti-forgery and <c>Refund.Create</c> gate, and PRGs back
/// to the hub tab. Owner scoping is enforced by the API (Finance module).
/// </summary>
[Area("Accounts")]
[Authorize]
public sealed class DisputesController : BaseController
{
    private readonly DisputesFacade _disputes;

    public DisputesController(DisputesFacade disputes)
    {
        _disputes = disputes;
    }

    [HttpGet("accounts/disputes")]
    public IActionResult Index(int page = 1)
        => RedirectPermanent(Url.Action(
            "Index", "Payments",
            page > 1
                ? new { area = "Accounts", tab = "disputes", page }
                : (object)new { area = "Accounts", tab = "disputes" })!);

    [HttpPost("accounts/disputes")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Refund.Create)]
    public async Task<IActionResult> Open(OpenDisputeFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please correct the highlighted fields and try again.");
            return BackToTab();
        }

        var result = await _disputes.OpenAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your dispute has been submitted. Our team will review it shortly.");
        else
            SetError(result.Error ?? "Could not open the dispute.");

        return BackToTab();
    }

    private IActionResult BackToTab()
        => RedirectToAction("Index", "Payments", new { area = "Accounts", tab = "disputes" });
}
