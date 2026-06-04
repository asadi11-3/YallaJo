using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Payments;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.AdminFinanceDashboard.Read)]
public sealed class PaymentsController : BaseController
{
    private readonly PaymentsFacade _payments;

    public PaymentsController(PaymentsFacade payments) => _payments = payments;

    [HttpGet("admin/finance")]
    public async Task<IActionResult> Index([FromQuery] PaymentsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "Finance";

        var result = await _payments.GetEarningsAsync(request, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new PaymentsVm());
        }

        return View(result.Data);
    }
}
