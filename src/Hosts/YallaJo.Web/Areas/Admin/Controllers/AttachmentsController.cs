using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Attachments;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Attachment.Read)]
public sealed class AttachmentsController : BaseController
{
    private readonly AttachmentsFacade _facade;
    public AttachmentsController(AttachmentsFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Index(
        EntityTypeOption? entityType, Guid? entityId, CancellationToken ct)
    {
        if (entityType is null || entityId is null || entityId == Guid.Empty)
            return View(new AttachmentListVm());

        var filter = new AttachmentListFilterVm
        {
            EntityType = entityType.Value,
            EntityId   = entityId.Value,
        };

        var result = await _facade.GetForEntityAsync(filter, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess)
        {
            ViewBag.Error = result.Error;
            return View(new AttachmentListVm { Filter = filter, HasFilter = true });
        }
        return View(result.Data);
    }

    [HttpPost("admin/attachments/upload")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Attachment.Create)]
    public async Task<IActionResult> Upload(UploadAttachmentVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError("Please fix the upload form errors.");
            return RedirectToAction(nameof(Index),
                new { entityType = vm.EntityType, entityId = vm.EntityId });
        }

        var result = await _facade.UploadAsync(vm, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Attachment uploaded.", "Upload failed.");

        return RedirectToAction(nameof(Index),
            new { entityType = vm.EntityType, entityId = vm.EntityId });
    }

    [HttpPost("admin/attachments/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Attachment.Delete)]
    public async Task<IActionResult> Delete(
        Guid id, EntityTypeOption entityType, Guid entityId, CancellationToken ct)
    {
        var result = await _facade.DeleteAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Attachment deleted.", "Delete failed.");

        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }

    [HttpPost("admin/attachments/{id:guid}/set-primary")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EntityImage.Update)]
    public async Task<IActionResult> SetPrimary(
        Guid id, EntityTypeOption entityType, Guid entityId, CancellationToken ct)
    {
        var result = await _facade.SetPrimaryAsync(entityType, entityId, id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        SetFlash(result, "Primary image set.", "Could not set primary image.");

        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }
}
