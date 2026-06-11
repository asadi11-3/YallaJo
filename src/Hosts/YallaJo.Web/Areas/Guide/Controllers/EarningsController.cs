using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Earnings;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class EarningsController : GuideBaseController
{
    private const int DefaultPageSize = 20;

    private readonly GuideEarningsFacade _earnings;

    public EarningsController(GuideEarningsFacade earnings) => _earnings = earnings;

    [HttpGet("guide/earnings")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        SetNav("Earnings");

        var result = await _earnings.GetAsync(page, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        EarningsVm vm;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            vm = new EarningsVm();
        }
        else
        {
            vm = result.Data;
        }

        if (WantsAjax())
        {
            return PartialView("_EarningsResults", vm);
        }

        return View(vm);
    }
}
