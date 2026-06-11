using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Tags;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Tag.Read)]
public sealed class TagsController : BaseController
{
    private readonly TagsFacade _facade;
    public TagsController(TagsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _facade.GetTagsAsync(activeOnly, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
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
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Tag created.");
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
        if (GuardSignOut(list) is { } signOut) return signOut;
        if (!list.IsSuccess || list.Data is null)
        {
            SetError(list.Error ?? "Could not load tags.");
            return RedirectToAction(nameof(Index));
        }
        var row = list.Data.Tags.FirstOrDefault(t => t.Id == id);
        if (row is null)
        {
            SetError("Tag not found.");
            return RedirectToAction(nameof(Index));
        }

        // PE1: deep links render the Index with the edit modal server-side open.
        return View(nameof(Index), new TagListVm
        {
            Tags = list.Data.Tags,
            EditId = id,
            Edit = new UpdateTagVm { Id = row.Id, Name = row.Name, Slug = row.Slug },
        });
    }

    [HttpPost("admin/tags/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tag.Update)]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = "Edit")] UpdateTagVm vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return await ReloadIndexForEdit(vm, ct);

        var result = await _facade.UpdateAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            SetSuccess("Tag updated.");
            return RedirectToAction(nameof(Index));
        }

        if (result.ValidationErrors is not null)
        {
            foreach (var (field, messages) in result.ValidationErrors)
                foreach (var m in messages)
                    ModelState.AddModelError($"Edit.{field}", m);
            return await ReloadIndexForEdit(vm, ct);
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Could not update tag.");
        return await ReloadIndexForEdit(vm, ct);
    }

    [HttpPost("admin/tags/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Tag.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Tag deleted.", "Could not delete tag.");
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

    private async Task<IActionResult> ReloadIndexForEdit(UpdateTagVm edit, CancellationToken ct)
    {
        var list = await _facade.GetTagsAsync(activeOnly: false, ct);
        var vm = new TagListVm
        {
            Tags = list.IsSuccess && list.Data is not null ? list.Data.Tags : [],
            EditId = edit.Id,
            Edit = edit,
        };
        return View(nameof(Index), vm);
    }
}
