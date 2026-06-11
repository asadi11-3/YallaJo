using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.SeoRedirects;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Redirect.Read)]
public sealed class SeoRedirectsController : BaseController
{
    private readonly SeoRedirectsFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SeoRedirectsController(SeoRedirectsFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] RedirectsFilterRequest request, CancellationToken ct)
    {
        ViewData["AdminNav"] = "SeoRedirects";
        var result = await _facade.GetIndexAsync(request, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new RedirectsVm());
        }

        return View(result.Data);
    }

    [HttpPost("admin/seo/redirects/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Redirect.Create)]
    public async Task<IActionResult> Create(CreateRedirectFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError(_localizer["Admin.SeoRedirects.Flash.CreateFieldsRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.CreateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.SeoRedirects.Flash.Created"].Value, _localizer["Admin.SeoRedirects.Flash.CreateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/seo/redirects/update")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Redirect.Update)]
    public async Task<IActionResult> Update(UpdateRedirectFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError(_localizer["Admin.SeoRedirects.Flash.UpdateFieldsRequired"].Value);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.UpdateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.SeoRedirects.Flash.Updated"].Value, _localizer["Admin.SeoRedirects.Flash.UpdateFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("admin/seo/redirects/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Redirect.Delete)]
    public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        SetFlash(result, _localizer["Admin.SeoRedirects.Flash.Deleted"].Value, _localizer["Admin.SeoRedirects.Flash.DeleteFailed"].Value);
        return RedirectToAction(nameof(Index));
    }
}
