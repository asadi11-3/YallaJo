using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Content.Facades;
using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Content.Controllers;

[Area("Content")]
[AllowAnonymous]
public sealed class BlogsController : BaseController
{
    private const int DefaultPageSize = 9;

    private readonly BlogsFacade _facade;

    public BlogsController(BlogsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = DefaultPageSize,
        string? search = null,
        bool? isFeatured = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 50) pageSize = DefaultPageSize;

        var result = await _facade.GetListAsync(page, pageSize, search, isFeatured, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new BlogListVm());
        }

        return View(result.Data);
    }

    [HttpGet("content/blogs/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return RedirectToAction(nameof(Index));

        var result = await _facade.GetDetailsAsync(slug, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsNotFound)
            return NotFound();

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }
}
