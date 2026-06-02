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
    public IActionResult Create() => View(new CreateBlogVm());

    // ── POST /admin/blogs/create ────────────────────────────────────────────────────
    [HttpPost("admin/blogs/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Blog.Create)]
    public async Task<IActionResult> Create(CreateBlogVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.CreateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess && result.Data is not null)
        {
            SetSuccess("Blog created as Draft.");
            return RedirectToAction(nameof(Edit), new { id = result.Data.BlogId });
        }

        if (ApplyValidationErrors(result)) return View(vm);

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create the blog.");
        return View(vm);
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
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Blog updated.");
            return RedirectToAction(nameof(Edit), new { id });
        }

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
}
