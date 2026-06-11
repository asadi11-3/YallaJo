using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Recommendations;
using YallaJo.Web.Areas.Accounts.Shared;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Accounts.Controllers;

[Area("Accounts")]
[Authorize]
[RequirePermission(WebPermission.Recommendation.Read)]
public sealed class RecommendationsController : BaseController
{
    private readonly RecommendationsFacade _recommendations;
    private readonly ProfileFacade _profile;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RecommendationsController(RecommendationsFacade recommendations, ProfileFacade profile, IStringLocalizer<SharedResource> localizer)
    {
        _recommendations = recommendations;
        _profile = profile;
        _localizer = localizer;
    }

    [HttpGet("accounts/recommendations")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["AccountNav"] = "Recommendations";
        await PopulateSidebarAsync(ct);

        var result = await _recommendations.GetAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new RecommendationsVm());
        }

        return View(result.Data);
    }

    [HttpPost("accounts/recommendations/preferences")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Preference.Update)]
    public async Task<IActionResult> UpdatePreferences(PreferencesFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError(_localizer["Accounts.Msg.FormError"]);
            return RedirectToAction(nameof(Index));
        }

        var result = await _recommendations.UpdatePreferencesAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.PreferencesSaved"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.PreferencesSaveFailed"].Value);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/recommendations/not-interested")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Preference.Update)]
    public async Task<IActionResult> NotInterested(RecommendationEntityType kind, Guid entityId, CancellationToken ct)
    {
        var result = await _recommendations.MarkNotInterestedAsync(kind, entityId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        // Phase 4c: optimistic dismiss (NF6) posts via the shared api client and only needs a status signal.
        if (WantsAjax())
            return result.IsSuccess
                ? Ok(new { dismissed = true })
                : BadRequest(new { error = result.Error ?? _localizer["Accounts.Msg.RecommendationUpdateFailed"].Value });

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.NotInterestedDone"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.RecommendationUpdateFailed"].Value);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("accounts/recommendations/onboarding")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Preference.Update)]
    public async Task<IActionResult> Onboarding(OnboardingFormVm form, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            SetError(_localizer["Accounts.Msg.OnboardingPickError"]);
            return RedirectToAction(nameof(Index));
        }

        var result = await _recommendations.SubmitOnboardingAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess(_localizer["Accounts.Msg.OnboardingSaved"]);
        else
            SetError(result.Error ?? _localizer["Accounts.Msg.OnboardingFailed"].Value);

        return RedirectToAction(nameof(Index));
    }

    // AJAX endpoint for lightweight interaction tracking (view/click), mirrors the favorites toggle pattern.
    [HttpPost("accounts/recommendations/track")]
    [ValidateAntiForgeryToken]
    [RequirePermission(WebPermission.Interaction.Create)]
    public async Task<IActionResult> Track(string entityType, Guid entityId, string interactionType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(interactionType) || entityId == Guid.Empty)
        {
            return BadRequest(new { error = "Invalid interaction payload." });
        }

        var result = await _recommendations.RecordInteractionAsync(entityType, entityId, interactionType, ct);
        if (result.RequireSignOut) return Unauthorized(new { error = "Session expired." });
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode == 0 ? 500 : result.StatusCode, new { error = result.Error });
        }
        return Ok(new { tracked = true });
    }

    private async Task PopulateSidebarAsync(CancellationToken ct)
    {
        var profile = await _profile.GetAsync(ct);
        if (profile is { IsSuccess: true, Data: { } p })
        {
            ViewBag.Sidebar = new AccountSidebarVm
            {
                AvatarUrl = p.AvatarUrl,
                DisplayName = string.IsNullOrWhiteSpace(p.DisplayName)
                    ? $"{p.FirstName} {p.LastName}".Trim()
                    : p.DisplayName,
                Email = p.Email,
            };
        }
        else
        {
            ViewBag.Sidebar = new AccountSidebarVm();
        }
    }
}
