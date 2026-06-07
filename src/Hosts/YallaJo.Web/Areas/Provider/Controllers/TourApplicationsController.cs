using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Identity;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize]
public sealed class TourApplicationsController : BaseController
{
    private readonly ProviderTourApplicationsFacade _facade;
    private readonly ICurrentUser _currentUser;

    public TourApplicationsController(ProviderTourApplicationsFacade facade, ICurrentUser currentUser)
    {
        _facade = facade;
        _currentUser = currentUser;
    }

    // ── GET /provider/tours/{id}/applications ─────────────────────────────────────
    [HttpGet("provider/tours/{id:guid}/applications")]
    public async Task<IActionResult> Index(Guid id, int page = 1, CancellationToken ct = default)
    {
        if (!_currentUser.HasPermission(WebPermission.GuideApplication.Read))
            return RedirectToStatus();

        var result = await _facade.GetIndexAsync(id, page, ct);
        return result.Outcome switch
        {
            TourApplicationOutcome.Ok => View(result.Data),
            TourApplicationOutcome.ForceSignOut => RedirectToLogin(),
            TourApplicationOutcome.Forbidden => Denied(result.Error),
            _ => NotFoundRedirect(result.Error),
        };
    }

    // ── POST /provider/tours/{id}/applications/{applicationId}/approve ────────────
    [HttpPost("provider/tours/{id:guid}/applications/{applicationId:guid}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, Guid applicationId, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.GuideApplication.Approve))
            return RedirectToStatus();

        var result = await _facade.ApproveAsync(id, applicationId, ct);
        return Finish(result, id, "Application approved.");
    }

    // ── POST /provider/tours/{id}/applications/{applicationId}/reject ─────────────
    [HttpPost("provider/tours/{id:guid}/applications/{applicationId:guid}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, Guid applicationId, string? reason, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.GuideApplication.Reject))
            return RedirectToStatus();

        if (string.IsNullOrWhiteSpace(reason))
        {
            SetError("A reason is required to reject an application.");
            return RedirectToAction(nameof(Index), new { id });
        }

        var result = await _facade.RejectAsync(id, applicationId, reason.Trim(), ct);
        return Finish(result, id, "Application rejected.");
    }

    // ── POST /provider/tours/{id}/applications/open ───────────────────────────────
    [HttpPost("provider/tours/{id:guid}/applications/open")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.Update))
            return RedirectToStatus();

        var result = await _facade.OpenAsync(id, ct);
        return Finish(result, id, "Tour opened for guide applications.");
    }

    // ── POST /provider/tours/{id}/applications/close ──────────────────────────────
    [HttpPost("provider/tours/{id:guid}/applications/close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(WebPermission.Tour.Update))
            return RedirectToStatus();

        var result = await _facade.CloseAsync(id, ct);
        return Finish(result, id, "Tour closed for guide applications.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private IActionResult Finish(TourApplicationActionResult result, Guid id, string success)
    {
        if (result.Outcome == TourApplicationOutcome.ForceSignOut) return RedirectToLogin();

        if (result.Outcome == TourApplicationOutcome.Ok)
            SetSuccess(success);
        else
            SetError(result.Error ?? "The action could not be completed.");

        return RedirectToAction(nameof(Index), new { id });
    }

    private IActionResult Denied(string? message)
    {
        SetError(message ?? "You don't have access to this listing.");
        return RedirectToAction("Index", "Tours", new { area = "Provider" });
    }

    private IActionResult NotFoundRedirect(string? message)
    {
        SetError(message ?? "Not found.");
        return RedirectToAction("Index", "Tours", new { area = "Provider" });
    }

    private IActionResult RedirectToStatus() =>
        RedirectToAction("Status", "Provider", new { area = "Provider" });
}
