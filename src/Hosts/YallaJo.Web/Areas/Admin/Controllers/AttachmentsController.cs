using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Models.Attachments;
using YallaJo.Web.Infrastructure.Authorization;

using YallaJo.Web.Areas.Admin.Facades;
namespace YallaJo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[RequirePermission(WebPermission.Attachment.Read)]
public sealed class AttachmentsController : Controller
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
        if (result.RequireSignOut) return RedirectToLogin();
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
            TempData["Error"] = "Please fix the upload form errors.";
            return RedirectToAction(nameof(Index),
                new { entityType = vm.EntityType, entityId = vm.EntityId });
        }

        var result = await _facade.UploadAsync(vm, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Attachment uploaded." : result.Error ?? "Upload failed.";

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
        if (result.RequireSignOut) return RedirectToLogin();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Attachment deleted." : result.Error ?? "Delete failed.";

        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }

    [HttpPost("admin/attachments/{id:guid}/set-primary")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.EntityImage.Update)]
    public async Task<IActionResult> SetPrimary(
        Guid id, EntityTypeOption entityType, Guid entityId, CancellationToken ct)
    {
        var result = await _facade.SetPrimaryAsync(entityType, entityId, id, ct);
        if (result.RequireSignOut) return RedirectToLogin();

        TempData[result.IsSuccess ? "Success" : "Error"] =
            result.IsSuccess ? "Primary image set." : result.Error ?? "Could not set primary image.";

        return RedirectToAction(nameof(Index), new { entityType, entityId });
    }

    private IActionResult RedirectToLogin()
        => RedirectToAction("SignIn", "Auth", new { area = "Auth" });
}
