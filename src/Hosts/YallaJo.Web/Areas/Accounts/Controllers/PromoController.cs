using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Accounts.Facades;
using YallaJo.Web.Areas.Accounts.Models.Promo;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Accounts.Controllers;

/// <summary>
/// AJAX proxy endpoints for inline editing of My Profile promotional placements.
/// <para>
/// These actions are consumed by <c>profile-promo.js</c> and always return JSON.
/// The controller is <see cref="AuthorizeAttribute">[Authorize]</see> only; the real
/// role/permission gate lives on the API (<c>MustHavePermission(Promotion, Update)</c>),
/// so a normal user that calls these proxies still receives a 403 from the backend
/// (defense-in-depth — decision #6).
/// </para>
/// </summary>
[Area("Accounts")]
[Authorize]
[Route("accounts/promo")]
public sealed class PromoController : BaseController
{
    private readonly PromoFacade _facade;

    public PromoController(PromoFacade facade) => _facade = facade;

    /// <summary>Update a placement's content fields (title, description, CTA, status…).</summary>
    [HttpPost("{key}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string key, UpdatePromoBlockVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationJson(CollectModelStateErrors());
        }

        var result = await _facade.UpdateAsync(key, vm, ct);
        return ToJson(result);
    }

    /// <summary>Upload/replace a placement's image. Returns the refreshed block on success.</summary>
    [HttpPost("{key}/image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(string key, IFormFile? file, CancellationToken ct)
    {
        var result = await _facade.UploadImageAsync(key, file, ct);
        return ToJson(result);
    }

    /// <summary>
    /// Maps an <see cref="ApiResult{T}"/> to a JSON envelope the front-end understands:
    /// <c>{ success, promo? , error?, errors? }</c>. Sign-out becomes a 401 JSON body
    /// (the JS layer redirects to login) instead of an MVC redirect a fetch can't follow.
    /// </summary>
    private IActionResult ToJson(ApiResult<PromoBlockVm> result)
    {
        if (result.IsSuccess && result.Data is not null)
        {
            return Json(new { success = true, promo = result.Data });
        }

        if (result.RequireSignOut)
        {
            return StatusCode(401, new { success = false, signOut = true, error = L["Public.Session.Expired"].Value });
        }

        if (result.IsValidationError && result.ValidationErrors is { Count: > 0 })
        {
            return ValidationJson(result.ValidationErrors);
        }

        return StatusCode(
            result.StatusCode == 0 ? 400 : result.StatusCode,
            new { success = false, error = result.Error ?? L["Accounts.Promo.SaveFailed"].Value });
    }

    private IActionResult ValidationJson(IReadOnlyDictionary<string, string[]> errors) =>
        BadRequest(new { success = false, errors });

    private Dictionary<string, string[]> CollectModelStateErrors() =>
        ModelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
}
