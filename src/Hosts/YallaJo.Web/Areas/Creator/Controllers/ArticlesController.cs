using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Creator.Facades;
using YallaJo.Web.Areas.Creator.Models.Articles;
using YallaJo.Web.Areas.Creator.Models.Articles.Images;
using YallaJo.Web.Areas.Creator.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Creator.Controllers;

/// <summary>
/// Creator article management (CCD-4).
/// <para>Permission gates (separate routes per action, each correctly scoped):
/// list = <c>Blog.ReadOwn</c>; create = <c>Blog.Create</c>; edit-prefetch =
/// <c>Blog.Read</c> (admin-get, the RowVersion source); update = <c>Blog.Update</c>;
/// submit = <c>Blog.Submit</c>; delete/restore = <c>Blog.DeleteOwn</c>.</para>
/// <para>Edit prefetch uses GET /api/v1/blogs/admin/{id} (RowVersion + Status); the
/// anonymous GET /api/v1/blogs/{id} is never used. Restore is offered only as an
/// immediate post-delete Undo (no deleted-articles list).</para>
/// </summary>
[Area("Creator")]
[Authorize]
public sealed class ArticlesController : BaseController
{
    private const string UndoIdKey = "ArticleUndoId";
    private const string UndoRowVersionKey = "ArticleUndoRowVersion";
    private const string UndoTitleKey = "ArticleUndoTitle";

    private readonly CreatorArticlesFacade _facade;
    private readonly CreatorArticleImagesFacade _images;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ArticlesController(
        CreatorArticlesFacade facade,
        CreatorArticleImagesFacade images,
        IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _images = images;
        _localizer = localizer;
    }

    // GET /creator/articles
    [HttpGet("creator/articles")]
    [RequirePermission(WebPermission.Blog.ReadOwn)]
    public async Task<IActionResult> Index(int page = 1, string? status = null, CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _facade.GetMyArticlesAsync(page, status, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return WantsAjax() ? PartialView("_ArticlesResults", new MyArticlesVm()) : View(new MyArticlesVm());
        }

        var vm = result.Data;

        // Surface a one-time post-delete Undo banner if a delete just happened.
        if (TempData[UndoIdKey] is string idStr && Guid.TryParse(idStr, out var undoId))
        {
            vm = new MyArticlesVm
            {
                Articles       = vm.Articles,
                StatusFilter   = vm.StatusFilter,
                StatusOptions  = vm.StatusOptions,
                Pager          = vm.Pager,
                UndoArticleId  = undoId,
                UndoRowVersion = TempData[UndoRowVersionKey] as string,
                UndoTitle      = TempData[UndoTitleKey] as string,
            };
        }

        // S1/PE1: AJAX refinement returns the results fragment; plain GET renders the full page.
        return WantsAjax() ? PartialView("_ArticlesResults", vm) : View(vm);
    }

    // GET /creator/articles/new
    [HttpGet("creator/articles/new")]
    [RequirePermission(WebPermission.Blog.Create)]
    public IActionResult New()
    {
        SetSidebar();
        return View("Editor", new ArticleEditorVm());
    }

    // POST /creator/articles/new
    [HttpPost("creator/articles/new")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Create)]
    public async Task<IActionResult> New(ArticleEditorVm form, CancellationToken ct = default)
    {
        SetSidebar();

        if (!ModelState.IsValid)
            return View("Editor", form);

        var result = await _facade.CreateAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
                ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Creator.Articles.Flash.CreateFailed"].Value);
            return View("Editor", form);
        }

        SetSuccess(_localizer["Creator.Articles.Flash.DraftCreated"]);
        return RedirectToAction(nameof(Edit), new { id = result.Data });
    }

    // GET /creator/articles/{id}/edit  — admin-get prefetch (RowVersion + Status)
    [HttpGet("creator/articles/{id:guid}/edit")]
    [RequirePermission(WebPermission.Blog.Read)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        SetSidebar();

        var result = await _facade.GetEditorAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        var vm = result.Data;

        // Load the article's images (best-effort: a failure here must not block editing).
        var imagesResult = await _images.GetImagesAsync(id, ct);
        if (imagesResult is { IsSuccess: true, Data: { } imagesVm })
            vm.Images = imagesVm;

        // Resolve human-readable names for the linked-tour chips (best-effort, bounded).
        if (vm.HasLinkedTours)
            vm.LinkedTours = await _facade.ResolveTourNamesAsync(vm.LinkedTours, ct);

        return View("Editor", vm);
    }

    // POST /creator/articles/{id}/edit  — Blog.Update (RowVersion)
    [HttpPost("creator/articles/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Update)]
    public async Task<IActionResult> Edit(Guid id, ArticleEditorVm form, CancellationToken ct = default)
    {
        SetSidebar();
        form.Id = id;

        if (!ModelState.IsValid)
            return View("Editor", form);

        var result = await _facade.UpdateAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
                ModelState.AddModelError(string.Empty, result.Error ?? _localizer["Creator.Articles.Flash.UpdateFailed"].Value);
            return View("Editor", form);
        }

        SetSuccess(_localizer["Creator.Articles.Flash.Updated"]);
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /creator/articles/{id}/submit  — Blog.Submit (RowVersion)
    [HttpPost("creator/articles/{id:guid}/submit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Submit)]
    public async Task<IActionResult> Submit(Guid id, string rowVersion, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(rowVersion))
        {
            SetError(_localizer["Creator.Articles.Flash.MissingVersion"]);
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.SubmitForReviewAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Articles.Flash.Submitted"].Value, _localizer["Creator.Articles.Flash.SubmitFailed"].Value);
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /creator/articles/{id}/delete  — Blog.DeleteOwn (RowVersion, modal-confirmed)
    [HttpPost("creator/articles/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.DeleteOwn)]
    public async Task<IActionResult> Delete(Guid id, string rowVersion, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(rowVersion))
        {
            SetError(_localizer["Creator.Articles.Flash.MissingVersion"]);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.DeleteAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        // Offer an immediate Undo: re-fetch the (now soft-deleted) article's RowVersion
        // so restore can be issued. Best-effort — if it fails, just show success.
        var rvResult = await _facade.GetRowVersionAsync(id, ct);
        if (rvResult is { IsSuccess: true, Data: var data })
        {
            TempData[UndoIdKey] = id.ToString();
            TempData[UndoRowVersionKey] = data.RowVersion;
            TempData[UndoTitleKey] = data.Title;
        }

        SetSuccess(_localizer["Creator.Articles.Flash.Deleted"]);
        return RedirectToAction(nameof(Index));
    }

    // POST /creator/articles/{id}/restore  — Blog.DeleteOwn (RowVersion; post-delete Undo)
    [HttpPost("creator/articles/{id:guid}/restore")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.DeleteOwn)]
    public async Task<IActionResult> Restore(Guid id, string rowVersion, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(rowVersion))
        {
            SetError(_localizer["Creator.Articles.Flash.RestoreUnavailable"]);
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.RestoreAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Articles.Flash.Restored"].Value, _localizer["Creator.Articles.Flash.RestoreFailed"].Value);
        return RedirectToAction(nameof(Index));
    }

    // ── Blog ↔ Tour links (§7.2) — RowVersion-guarded, separate from text-save ──

    // GET /creator/articles/tour-lookup?q=  — type-ahead source for the link combobox.
    // JSON only; the combobox degrades to a plain name input (resolved server-side) without JS.
    [HttpGet("creator/articles/tour-lookup")]
    [RequirePermission(WebPermission.BlogTourLink.Create)]
    public async Task<IActionResult> TourLookup(string? q, CancellationToken ct = default)
    {
        var matches = await _facade.SuggestToursAsync(q, ct);
        return Json(new { items = matches.Select(t => new { id = t.Id, name = t.Name, slug = t.Slug }) });
    }

    // POST /creator/articles/{id}/tours  — BlogTourLink.Create (RowVersion)
    // JS path posts the resolved GUID in `tourId`; the no-JS combobox posts the typed
    // name in `tourQuery`, resolved here via the suggest endpoint (PE1).
    [HttpPost("creator/articles/{id:guid}/tours")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BlogTourLink.Create)]
    public async Task<IActionResult> LinkTour(
        Guid id, Guid? tourId, string? tourQuery, string rowVersion, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(rowVersion))
        {
            SetError(_localizer["Creator.Articles.Flash.MissingVersion"]);
            return RedirectToAction(nameof(Edit), new { id });
        }

        var resolvedTourId = tourId is { } tid && tid != Guid.Empty
            ? tid
            : await _facade.ResolveTourIdByNameAsync(tourQuery, ct);

        if (resolvedTourId is not { } linkTourId || linkTourId == Guid.Empty)
        {
            SetError(_localizer["Creator.Articles.Flash.PickTour"]);
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.LinkTourAsync(id, linkTourId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Articles.Flash.TourLinked"].Value, _localizer["Creator.Articles.Flash.TourLinkFailed"].Value);
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /creator/articles/{id}/tours/{tourId}/unlink  — BlogTourLink.Delete (RowVersion)
    [HttpPost("creator/articles/{id:guid}/tours/{tourId:guid}/unlink")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BlogTourLink.Delete)]
    public async Task<IActionResult> UnlinkTour(Guid id, Guid tourId, string rowVersion, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(rowVersion))
        {
            SetError(_localizer["Creator.Articles.Flash.MissingVersion"]);
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.UnlinkTourAsync(id, tourId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Articles.Flash.TourUnlinked"].Value, _localizer["Creator.Articles.Flash.TourUnlinkFailed"].Value);
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── Article images (CCD-5) — separate multipart flow; never mixed with text-save ──

    // POST /creator/articles/{id}/images/upload  — Attachment.Create
    [HttpPost("creator/articles/{id:guid}/images/upload")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Attachment.Create)]
    public async Task<IActionResult> UploadImages(
        Guid id, List<IFormFile> files, int existingCount, CancellationToken ct = default)
    {
        var result = await _images.UploadAsync(id, files ?? [], existingCount, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Articles.Flash.ImagesUploaded"].Value, result.Error ?? _localizer["Creator.Articles.Flash.ImagesUploadFailed"].Value);
        return await ImagesResultAsync(id, ct);
    }

    // POST /creator/articles/{id}/images/{attachmentId}/delete  — Attachment.Delete
    [HttpPost("creator/articles/{id:guid}/images/{attachmentId:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Attachment.Delete)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid attachmentId, CancellationToken ct = default)
    {
        var result = await _images.DeleteAsync(attachmentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Articles.Flash.ImageDeleted"].Value, _localizer["Creator.Articles.Flash.ImageDeleteFailed"].Value);
        return await ImagesResultAsync(id, ct);
    }

    // POST /creator/articles/{id}/images/{attachmentId}/primary  — EntityImage.Update
    [HttpPost("creator/articles/{id:guid}/images/{attachmentId:guid}/primary")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EntityImage.Update)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid attachmentId, CancellationToken ct = default)
    {
        var result = await _images.SetPrimaryAsync(id, attachmentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Articles.Flash.PrimarySet"].Value, _localizer["Creator.Articles.Flash.PrimaryFailed"].Value);
        return await ImagesResultAsync(id, ct);
    }

    // POST /creator/articles/{id}/images/reorder  — Attachment.Update
    [HttpPost("creator/articles/{id:guid}/images/reorder")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Attachment.Update)]
    public async Task<IActionResult> ReorderImages(
        Guid id, List<Guid> orderedAttachmentIds, CancellationToken ct = default)
    {
        if (orderedAttachmentIds is null || orderedAttachmentIds.Count == 0)
        {
            SetError(_localizer["Creator.Articles.Flash.NoImageOrder"]);
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _images.ReorderAsync(id, orderedAttachmentIds, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Creator.Articles.Flash.ImagesReordered"].Value, _localizer["Creator.Articles.Flash.ReorderFailed"].Value);
        return await ImagesResultAsync(id, ct);
    }

    // For AJAX (PE1 progressive enhancement) return the refreshed gallery fragment;
    // otherwise fall back to the classic PRG redirect so no-JS clients keep working.
    private async Task<IActionResult> ImagesResultAsync(Guid id, CancellationToken ct)
    {
        if (WantsAjax())
        {
            var images = await _images.GetImagesAsync(id, ct);
            return PartialView("_ArticleImages", images.Data ?? new ArticleImagesVm { BlogId = id });
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    private void SetSidebar()
    {
        ViewData["CreatorNav"] = "Articles";
        ViewBag.Sidebar = new CreatorSidebarVm { DisplayName = User.Identity?.Name ?? "Creator" };
    }
}
