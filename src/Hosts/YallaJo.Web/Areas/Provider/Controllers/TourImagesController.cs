using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.TourImages;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class TourImagesController : ProviderTourResourceController
{
    private readonly ProviderTourImagesFacade _facade;
    private readonly ICurrentUser _currentUser;

    public TourImagesController(ProviderTourImagesFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/tours/{id}/images ───────────────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/images")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        // Deliberate double-check: the gallery requires BOTH the tour-editor permission
        // and attachment read access (images are ContentCore attachments). Keep imperative.
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn)
            || !_currentUser.HasPermission(WebPermission.Attachment.Read))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(id, ct);
        return result.Outcome switch
        {
            TourImagesOutcome.Ok => View(result.Data),
            TourImagesOutcome.ForceSignOut => RedirectToLogin(),
            TourImagesOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error, notFoundFallback: L["Provider.Flash.ListingNotFound"].Value),
        };
    }

    // ── POST /provider/tours/{id}/images/upload ───────────────────────────────────
    [HttpPost("provider/tours/{id:guid}/images/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid id, TourImageUploadVm vm, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Attachment.Create))
            return RedirectToStatus();

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
            return await FailAsync(id, firstError ?? L["Provider.Flash.InvalidImage"].Value, ct);
        }

        var result = await _facade.UploadAsync(id, vm, ct);
        if (result.Outcome == TourImagesOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourImagesOutcome.Ok)
            return await SucceedAsync(id, L["Provider.Flash.ImageUploaded"].Value, ct);

        return await FailAsync(id, result.Error ?? L["Provider.Flash.CouldNotUploadImage"].Value, ct);
    }

    // ── POST /provider/tours/{id}/images/{attachmentId}/delete ─────────────────────
    [HttpPost("provider/tours/{id:guid}/images/{attachmentId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid attachmentId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Attachment.Delete))
            return RedirectToStatus();

        var result = await _facade.DeleteAsync(id, attachmentId, ct);
        if (result.Outcome == TourImagesOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourImagesOutcome.Ok)
            return await SucceedAsync(id, L["Provider.Flash.ImageDeleted"].Value, ct);

        return await FailAsync(id, result.Error ?? L["Provider.Flash.CouldNotDeleteImage"].Value, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// AJAX (WantsAjax): re-fetch the gallery and return the swappable fragment —
    /// the client toasts its own data-success-message (NF1). No-JS: PRG flash + redirect (PE1).
    /// </summary>
    private async Task<IActionResult> SucceedAsync(Guid id, string message, CancellationToken ct)
    {
        if (WantsAjax())
        {
            var refreshed = await _facade.GetIndexAsync(id, ct);
            if (refreshed.Outcome == TourImagesOutcome.ForceSignOut) return RedirectToLogin();
            return PartialView("_Gallery", refreshed.Data ?? new TourImagesVm { TourId = id });
        }

        SetSuccess(message);
        return RedirectToImages(id);
    }

    /// <summary>AJAX: 400 + { error } for the client toast. No-JS: PRG flash + redirect.</summary>
    private async Task<IActionResult> FailAsync(Guid id, string? message, CancellationToken ct)
    {
        if (WantsAjax())
            return BadRequest(new { error = message });

        SetError(message);
        await Task.CompletedTask;
        return RedirectToImages(id);
    }

    private IActionResult RedirectToImages(Guid id) =>
        RedirectToAction(nameof(Index), new { id });
}
