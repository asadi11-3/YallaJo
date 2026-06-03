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
public sealed class TourImagesController : BaseController
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
        if (!_currentUser.HasPermission(WebPermission.Tour.ReadOwn)
            || !_currentUser.HasPermission(WebPermission.Attachment.Read))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(id, ct);
        return result.Outcome switch
        {
            TourImagesOutcome.Ok => View(result.Data),
            TourImagesOutcome.ForceSignOut => RedirectToLogin(),
            TourImagesOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
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
            SetError(firstError ?? "Please choose a valid image to upload.");
            return RedirectToImages(id);
        }

        var result = await _facade.UploadAsync(id, vm, ct);
        if (result.Outcome == TourImagesOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourImagesOutcome.Ok)
            SetSuccess("Image uploaded.");
        else
            SetError(result.Error ?? "Could not upload the image.");

        return RedirectToImages(id);
    }

    // ── POST /provider/tours/{id}/images/{attachmentId}/delete ─────────────────────
    [HttpPost("provider/tours/{id:guid}/images/{attachmentId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid attachmentId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Attachment.Delete))
            return RedirectToStatus();

        var result = await _facade.DeleteAsync(attachmentId, ct);
        if (result.Outcome == TourImagesOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourImagesOutcome.Ok)
            SetSuccess("Image deleted.");
        else
            SetError(result.Error ?? "Could not delete the image.");

        return RedirectToImages(id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private IActionResult Denied(string? message)
    {
        SetError(message ?? "You don't have access to this listing.");
        return RedirectToAction("Index", "Tours", new { area = "Provider" });
    }

    private IActionResult NotFoundRedirect(string? message)
    {
        SetError(message ?? "Listing not found.");
        return RedirectToAction("Index", "Tours", new { area = "Provider" });
    }

    private IActionResult RedirectToImages(Guid id) =>
        RedirectToAction(nameof(Index), new { id });

    private IActionResult RedirectToStatus() =>
        RedirectToAction("Status", "Provider", new { area = "Provider" });
}
