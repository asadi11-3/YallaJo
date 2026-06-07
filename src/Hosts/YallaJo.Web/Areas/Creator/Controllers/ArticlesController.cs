using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Creator.Facades;
using YallaJo.Web.Areas.Creator.Models.Articles;
using YallaJo.Web.Areas.Creator.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

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

    public ArticlesController(CreatorArticlesFacade facade, CreatorArticleImagesFacade images)
    {
        _facade = facade;
        _images = images;
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
            return View(new MyArticlesVm());
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

        return View(vm);
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
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not create your article.");
            return View("Editor", form);
        }

        SetSuccess("Your draft article was created.");
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
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not update your article.");
            return View("Editor", form);
        }

        SetSuccess("Your article was updated.");
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
            SetError("Missing version token. Please refresh and try again.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.SubmitForReviewAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Your article was submitted for review.", "Could not submit your article for review.");
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
            SetError("Missing version token. Please refresh and try again.");
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

        SetSuccess("Your article was deleted.");
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
            SetError("This article can no longer be restored from here.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _facade.RestoreAsync(id, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Your article was restored.", "Could not restore your article.");
        return RedirectToAction(nameof(Index));
    }

    // ── Blog ↔ Tour links (§7.2) — RowVersion-guarded, separate from text-save ──

    // POST /creator/articles/{id}/tours  — BlogTourLink.Create (RowVersion)
    [HttpPost("creator/articles/{id:guid}/tours")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BlogTourLink.Create)]
    public async Task<IActionResult> LinkTour(Guid id, Guid tourId, string rowVersion, CancellationToken ct = default)
    {
        if (tourId == Guid.Empty)
        {
            SetError("Please provide a valid tour to link.");
            return RedirectToAction(nameof(Edit), new { id });
        }
        if (string.IsNullOrEmpty(rowVersion))
        {
            SetError("Missing version token. Please refresh and try again.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.LinkTourAsync(id, tourId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Tour linked to your article.", "Could not link the tour.");
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
            SetError("Missing version token. Please refresh and try again.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.UnlinkTourAsync(id, tourId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Tour unlinked from your article.", "Could not unlink the tour.");
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

        SetFlash(result, "Image(s) uploaded.", result.Error ?? "Could not upload image(s).");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /creator/articles/{id}/images/{attachmentId}/delete  — Attachment.Delete
    [HttpPost("creator/articles/{id:guid}/images/{attachmentId:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Attachment.Delete)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid attachmentId, CancellationToken ct = default)
    {
        var result = await _images.DeleteAsync(attachmentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Image deleted.", "Could not delete the image.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /creator/articles/{id}/images/{attachmentId}/primary  — EntityImage.Update
    [HttpPost("creator/articles/{id:guid}/images/{attachmentId:guid}/primary")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EntityImage.Update)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid attachmentId, CancellationToken ct = default)
    {
        var result = await _images.SetPrimaryAsync(id, attachmentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Primary image updated.", "Could not set the primary image.");
        return RedirectToAction(nameof(Edit), new { id });
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
            SetError("No image order was provided.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _images.ReorderAsync(id, orderedAttachmentIds, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Image order updated.", "Could not reorder the images.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    private void SetSidebar()
    {
        ViewData["CreatorNav"] = "Articles";
        ViewBag.Sidebar = new CreatorSidebarVm { DisplayName = User.Identity?.Name ?? "Creator" };
    }
}
