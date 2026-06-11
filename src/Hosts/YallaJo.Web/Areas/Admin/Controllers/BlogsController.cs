using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Blogs;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Blog.Read)]
public sealed class BlogsController : BaseController
{
    private const int DefaultPageSize = 20;

    private readonly BlogsFacade _facade;

    public BlogsController(BlogsFacade facade) => _facade = facade;

    // ── GET /admin/blogs ──────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(
        BlogAdminTab tab = BlogAdminTab.Published,
        int page = 1,
        string? search = null,
        bool? isFeatured = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var result = await _facade.GetListAsync(tab, page, DefaultPageSize, search, isFeatured, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            ViewBag.Error = result.Error;
            return View(new BlogListVm { Tab = tab, Search = search, IsFeatured = isFeatured });
        }

        return View(result.Data);
    }

    // ── GET /admin/blogs/create ─────────────────────────────────────────────────────
    [HttpGet("admin/blogs/create")]
    [RequirePermission(WebPermission.Blog.Create)]
    public async Task<IActionResult> Create(CancellationToken ct)
        // PE1: deep links render the Index with the "new draft" modal server-side open.
        => await ReloadIndexForCreate(new CreateBlogVm(), ct);

    // ── POST /admin/blogs/create ────────────────────────────────────────────────────
    [HttpPost("admin/blogs/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Create)]
    public async Task<IActionResult> Create([Bind(Prefix = "Create")] CreateBlogVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await ReloadIndexForCreate(vm, ct);

        var result = await _facade.CreateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess && result.Data is not null)
        {
            SetSuccess("Blog created as Draft.");
            return RedirectToAction(nameof(Edit), new { id = result.Data.BlogId });
        }

        // UI-UX-F6: surface API validation errors on the modal's Create.-prefixed fields.
        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Create.{field}", m);
            return await ReloadIndexForCreate(vm, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create the blog.");
        return await ReloadIndexForCreate(vm, ct);
    }

    /// <summary>
    /// Re-renders the Index with the "new draft" modal open (F8 §4.7), preserving
    /// the user's typed values so validation failures never lose work.
    /// </summary>
    private async Task<IActionResult> ReloadIndexForCreate(CreateBlogVm create, CancellationToken ct)
    {
        var list = await _facade.GetListAsync(BlogAdminTab.Published, 1, DefaultPageSize, null, null, ct);
        var vm = list.IsSuccess && list.Data is not null ? list.Data : new BlogListVm();
        if (!list.IsSuccess) ViewBag.Error = list.Error;

        vm.CreateOpen = true;
        vm.Create = create;
        return View(nameof(Index), vm);
    }

    // ── GET /admin/blogs/{id}/edit ──────────────────────────────────────────────────
    [HttpGet("admin/blogs/{id:guid}/edit")]
    [RequirePermission(WebPermission.Blog.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var result = await _facade.GetForEditAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsNotFound) return NotFound();
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // ── POST /admin/blogs/{id}/edit ─────────────────────────────────────────────────
    [HttpPost("admin/blogs/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Update)]
    public async Task<IActionResult> Edit(Guid id, EditBlogVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid)
        {
            vm.PlaceOptions = await _facade.LoadPlaceOptionsAsync(ct);
            return View(vm);
        }

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Blog updated.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        vm.PlaceOptions = await _facade.LoadPlaceOptionsAsync(ct);
        if (ApplyValidationErrors(result)) return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update the blog.");
        return View(vm);
    }

    // ── POST /admin/blogs/{id}/delete ───────────────────────────────────────────────
    [HttpPost("admin/blogs/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.DeleteOwn)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog deleted.", "Could not delete the blog.");
        return RedirectToAction(nameof(Index));
    }

    // ── POST /admin/blogs/{id}/restore ──────────────────────────────────────────────
    [HttpPost("admin/blogs/{id:guid}/restore")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.DeleteOwn)]
    public async Task<IActionResult> Restore(Guid id, string rowVersion, CancellationToken ct)
    {
        var result = await _facade.RestoreAsync(id, BlogsMapper.DecodeRowVersion(rowVersion), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog restored.", "Could not restore the blog.");
        return RedirectToAction(nameof(Index), new { tab = BlogAdminTab.Deleted });
    }

    // ── POST /admin/blogs/{id}/publish ──────────────────────────────────────────────
    [HttpPost("admin/blogs/{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Approve)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await _facade.PublishAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog published.", "Could not publish the blog.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── POST /admin/blogs/{id}/unpublish ────────────────────────────────────────────
    [HttpPost("admin/blogs/{id:guid}/unpublish")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Approve)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct)
    {
        var result = await _facade.UnpublishAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog unpublished (back to Draft).", "Could not unpublish the blog.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── POST /admin/blogs/{id}/archive ──────────────────────────────────────────────
    [HttpPost("admin/blogs/{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Approve)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await _facade.ArchiveAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog archived.", "Could not archive the blog.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── POST /admin/blogs/{id}/feature ──────────────────────────────────────────────
    [HttpPost("admin/blogs/{id:guid}/feature")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Feature)]
    public async Task<IActionResult> Feature(Guid id, DateTime? featuredUntil, CancellationToken ct)
    {
        var result = await _facade.FeatureAsync(id, featuredUntil, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog featured.", "Could not feature the blog.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── POST /admin/blogs/{id}/unfeature ────────────────────────────────────────────
    [HttpPost("admin/blogs/{id:guid}/unfeature")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Unfeature)]
    public async Task<IActionResult> Unfeature(Guid id, CancellationToken ct)
    {
        var result = await _facade.UnfeatureAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog unfeatured.", "Could not unfeature the blog.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── Moderation (Phase 2A) ───────────────────────────────────────────────────────

    // POST /admin/blogs/{id}/approve
    [HttpPost("admin/blogs/{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Approve)]
    public async Task<IActionResult> Approve(Guid id, bool fromEdit = false, CancellationToken ct = default)
    {
        var result = await _facade.ApproveAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog approved and published.", "Could not approve the blog.");
        return ModerationRedirect(id, fromEdit);
    }

    // POST /admin/blogs/{id}/reject
    [HttpPost("admin/blogs/{id:guid}/reject")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Reject)]
    public async Task<IActionResult> Reject(Guid id, string? reason, bool fromEdit = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A reason is required.");
            return ModerationRedirect(id, fromEdit);
        }

        var result = await _facade.RejectAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog rejected.", "Could not reject the blog.");
        return ModerationRedirect(id, fromEdit);
    }

    // Approve/Reject can be triggered from the Queue list or the Edit page;
    // redirect back to wherever the action was invoked.
    private IActionResult ModerationRedirect(Guid id, bool fromEdit) =>
        fromEdit
            ? RedirectToAction(nameof(Edit), new { id })
            : RedirectToAction(nameof(Index), new { tab = BlogAdminTab.Queue });

    // ── Moderation (Phase 2B) — Edit-page only, always redirect back to Edit ────────

    // POST /admin/blogs/{id}/hide
    [HttpPost("admin/blogs/{id:guid}/hide")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Approve)]
    public async Task<IActionResult> Hide(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A reason is required.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.HideAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog hidden.", "Could not hide the blog.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /admin/blogs/{id}/unhide
    [HttpPost("admin/blogs/{id:guid}/unhide")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Approve)]
    public async Task<IActionResult> Unhide(Guid id, CancellationToken ct)
    {
        var result = await _facade.UnhideAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog unhidden (back to Published).", "Could not unhide the blog.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /admin/blogs/{id}/remove
    [HttpPost("admin/blogs/{id:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Remove)]
    public async Task<IActionResult> Remove(Guid id, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A reason is required.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.RemoveAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Blog removed.", "Could not remove the blog.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // ── Tour linking (Phase 4) — Edit-page only ─────────────────────────────────────

    // POST /admin/blogs/{id}/tours
    [HttpPost("admin/blogs/{id:guid}/tours")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BlogTourLink.Create)]
    public async Task<IActionResult> LinkTour(Guid id, Guid tourId, CancellationToken ct)
    {
        if (tourId == Guid.Empty)
        {
            SetError("Please select a tour to link.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.LinkTourAsync(id, tourId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Tour linked.", "Could not link the tour.");
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /admin/blogs/{id}/tours/{tourId}/unlink
    [HttpPost("admin/blogs/{id:guid}/tours/{tourId:guid}/unlink")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.BlogTourLink.Delete)]
    public async Task<IActionResult> UnlinkTour(Guid id, Guid tourId, CancellationToken ct)
    {
        var result = await _facade.UnlinkTourAsync(id, tourId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Tour unlinked.", "Could not unlink the tour.");
        return RedirectToAction(nameof(Edit), new { id });
    }
}
