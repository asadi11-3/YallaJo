using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Packages;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class PackagesController(PackagesFacade packages) : BaseController
{
    [HttpGet("packages")]
    [OutputCache(PolicyName = "PublicShort", VaryByHeaderNames = new[] { "X-Requested-With" })]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await packages.GetGridAsync(page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            var fallback = new PackagesGridVm();
            return WantsAjax() ? PartialView("_PackagesResults", fallback) : View(fallback);
        }

        // Phase 7: AJAX requests (listing.js) receive just the results fragment.
        return WantsAjax() ? PartialView("_PackagesResults", result.Data) : View(result.Data);
    }

    [HttpGet("packages/{id:guid}")]
    [OutputCache(PolicyName = "PublicMedium")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct = default)
    {
        var result = await packages.GetDetailAsync(id, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
            {
                return NotFound();
            }

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        PublicOutputCacheTagger.AddTag(HttpContext, $"package:{id}");
        return View(result.Data);
    }
}
