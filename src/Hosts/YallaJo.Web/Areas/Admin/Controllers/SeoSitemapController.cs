using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.SeoSitemap;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Sitemap.Read)]
public sealed class SeoSitemapController : BaseController
{
    private readonly SeoSitemapFacade _facade;

    public SeoSitemapController(SeoSitemapFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] SitemapFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "SeoSitemap";
        var result = await _facade.GetIndexAsync(request, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new SeoSitemapVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/seo/sitemap/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Sitemap.Update)]
    public async Task<IActionResult> Update(UpdateSitemapEntryFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please provide a valid priority and change frequency.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UpdateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Sitemap entry updated.", "Could not update the sitemap entry.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/seo/sitemap/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Sitemap.Delete)]
    public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Sitemap entry deleted.", "Could not delete the sitemap entry.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/seo/sitemap/regenerate")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Sitemap.Refresh)]
    public async Task<IActionResult> Regenerate(CancellationToken ct)
    {
        var result = await _facade.RegenerateAsync(ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, "Sitemap regeneration triggered.", "Could not regenerate the sitemap.");
        return RedirectToAction(nameof(Index));
    }
}
