using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Blogs;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Blog.Read)]
public sealed class BlogsController : BaseController
{
    private const int DefaultPageSize = 20;

    private readonly BlogsFacade _facade;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BlogsController(BlogsFacade facade, IStringLocalizer<SharedResource> localizer)
    {
        _facade = facade;
        _localizer = localizer;
    }

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
            SetSuccess(_localizer["Admin.Blogs.Flash.CreatedDraft"].Value);
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
            SetSuccess(_localizer["Admin.Blogs.Flash.Updated"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.Deleted"].Value, _localizer["Admin.Blogs.Flash.DeleteFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.Restored"].Value, _localizer["Admin.Blogs.Flash.RestoreFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.Published"].Value, _localizer["Admin.Blogs.Flash.PublishFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.Unpublished"].Value, _localizer["Admin.Blogs.Flash.UnpublishFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.Archived"].Value, _localizer["Admin.Blogs.Flash.ArchiveFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.Featured"].Value, _localizer["Admin.Blogs.Flash.FeatureFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.Unfeatured"].Value, _localizer["Admin.Blogs.Flash.UnfeatureFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.ApprovedPublished"].Value, _localizer["Admin.Blogs.Flash.ApproveFailed"].Value);
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
            SetError(_localizer["Admin.Shared.Flash.ReasonRequired"].Value);
            return ModerationRedirect(id, fromEdit);
        }

        var result = await _facade.RejectAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Blogs.Flash.Rejected"].Value, _localizer["Admin.Blogs.Flash.RejectFailed"].Value);
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
            SetError(_localizer["Admin.Shared.Flash.ReasonRequired"].Value);
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.HideAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Blogs.Flash.Hidden"].Value, _localizer["Admin.Blogs.Flash.HideFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.Unhidden"].Value, _localizer["Admin.Blogs.Flash.UnhideFailed"].Value);
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
            SetError(_localizer["Admin.Shared.Flash.ReasonRequired"].Value);
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.RemoveAsync(id, reason.Trim(), ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Blogs.Flash.Removed"].Value, _localizer["Admin.Blogs.Flash.RemoveFailed"].Value);
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
            SetError(_localizer["Admin.Blogs.Flash.SelectTour"].Value);
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _facade.LinkTourAsync(id, tourId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, _localizer["Admin.Blogs.Flash.TourLinked"].Value, _localizer["Admin.Blogs.Flash.TourLinkFailed"].Value);
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

        SetFlash(result, _localizer["Admin.Blogs.Flash.TourUnlinked"].Value, _localizer["Admin.Blogs.Flash.TourUnlinkFailed"].Value);
        return RedirectToAction(nameof(Edit), new { id });
    }
}
