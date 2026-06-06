using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Packages;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class PackagesController(PackagesFacade packages) : BaseController
{
    [HttpGet("packages")]
    [OutputCache(PolicyName = "PublicShort")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await packages.GetGridAsync(page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new PackagesGridVm());
        }

        return View(result.Data);
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

        return View(result.Data);
    }
}
