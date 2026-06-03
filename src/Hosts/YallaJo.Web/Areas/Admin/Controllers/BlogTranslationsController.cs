using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.BlogTranslations;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Blog.Update)]
public sealed class BlogTranslationsController : BaseController
{
    private readonly BlogTranslationsFacade _facade;

    public BlogTranslationsController(BlogTranslationsFacade facade) => _facade = facade;

    // GET /admin/blogs/{id}/translations
    [HttpGet("admin/blogs/{id:guid}/translations")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetIndexAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsNotFound) return NotFound();
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction("Edit", "Blogs", new { id });
        }

        return View(result.Data);
    }

    // GET /admin/blogs/{id}/translations/{languageCode}/edit
    [HttpGet("admin/blogs/{id:guid}/translations/{languageCode}/edit")]
    public async Task<IActionResult> Edit(Guid id, string languageCode, CancellationToken ct)
    {
        var result = await _facade.GetEditAsync(id, languageCode, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index), new { id });
        }

        return View(result.Data);
    }

    // POST /admin/blogs/{id}/translations/{languageCode}
    [HttpPost("admin/blogs/{id:guid}/translations/{languageCode}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        Guid id, string languageCode, BlogTranslationEditVm vm, CancellationToken ct)
    {
        vm.BlogId = id;
        vm.LanguageCode = languageCode;

        if (!ModelState.IsValid) return View(nameof(Edit), vm);

        var result = await _facade.SaveAsync(id, languageCode, vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess($"Translation '{languageCode}' saved.");
            return RedirectToAction(nameof(Index), new { id });
        }

        if (ApplyValidationErrors(result)) return View(nameof(Edit), vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not save the translation.");
        return View(nameof(Edit), vm);
    }
}
