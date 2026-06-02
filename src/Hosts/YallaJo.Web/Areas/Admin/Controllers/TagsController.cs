using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Tags;
using YallaJo.Web.Infrastructure.Authorization;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Tag.Read)]
public sealed class TagsController : Controller
{
    private readonly TagsFacade _facade;
    public TagsController(TagsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _facade.GetTagsAsync(activeOnly, ct);
        if (result.RequireSignOut) return RedirectToLogin();
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new TagListVm { ActiveOnly = activeOnly });
        }
        return View(result.Data);
    }

    [HttpPost("admin/tags/create")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tag.Create)]
    public async Task<IActionResult> Create(CreateTagVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return await ReloadIndex(vm, ct);

        var result = await _facade.CreateAsync(vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Tag created.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Create.{field}", m);
            return await ReloadIndex(vm, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not create tag.");
        return await ReloadIndex(vm, ct);
    }

    [HttpGet("admin/tags/{id:guid}/edit")]
    [RequirePermission(WebPermission.Tag.Update)]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var list = await _facade.GetTagsAsync(activeOnly: false, ct);
        if (list.RequireSignOut) return RedirectToLogin();
        if (!list.IsSuccess || list.Data is null)
        {
            TempData["Error"] = list.Error ?? "Could not load tags.";
            return RedirectToAction(nameof(Index));
        }
        var row = list.Data.Tags.FirstOrDefault(t => t.Id == id);
        if (row is null)
        {
            TempData["Error"] = "Tag not found.";
            return RedirectToAction(nameof(Index));
        }
        return View(new UpdateTagVm { Id = row.Id, Name = row.Name, Slug = row.Slug });
    }

    [HttpPost("admin/tags/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tag.Update)]
    public async Task<IActionResult> Edit(Guid id, UpdateTagVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return View(vm);

        var result = await _facade.UpdateAsync(vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        if (result.IsSuccess)
        {
            TempData["Success"] = "Tag updated.";
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError(field, m);
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update tag.");
        return View(vm);
    }

    [HttpPost("admin/tags/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tag.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Tag deleted." : result.Error ?? "Could not delete tag.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ReloadIndex(CreateTagVm create, CancellationToken ct)
    {
        var list = await _facade.GetTagsAsync(activeOnly: false, ct);
        var vm = list.IsSuccess && list.Data is not null
            ? new TagListVm { Tags = list.Data.Tags, Create = create }
            : new TagListVm { Create = create };
        return View(nameof(Index), vm);
    }

    private IActionResult RedirectToLogin()
        => RedirectToAction("SignIn", "Auth", new { area = "Auth" });
}
