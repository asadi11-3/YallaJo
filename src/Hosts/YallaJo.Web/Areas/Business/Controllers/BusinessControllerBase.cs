using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Business.Shared;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Business.Controllers;

/// <summary>
/// Shared base for Business-area controllers. Hosts the sidebar wiring that was
/// previously duplicated across all six controllers (D-19) plus AJAX helpers (PE1, JS5).
/// </summary>
public abstract class BusinessControllerBase : BaseController
{
    protected void SetSidebar(string nav, Guid? businessId = null)
    {
        ViewData["BusinessNav"] = nav;
        ViewBag.Sidebar = new BusinessSidebarVm
        {
            DisplayName = User.Identity?.Name ?? "Business",
            BusinessId = businessId
        };
    }

    /// <summary>
    /// Sets a toast message header consumed by business-crud.js after an AJAX mutation (NF1).
    /// Header values must be ASCII-safe, so the message is URL-encoded; the client decodes it.
    /// </summary>
    protected void SetAjaxToast(string message) =>
        Response.Headers["X-Yj-Toast"] = Uri.EscapeDataString(message);

    /// <summary>
    /// Returns a 400 payload with a summary message and the ModelState field errors,
    /// matching the shape expected by api-client.js (error + errors dictionary).
    /// </summary>
    protected IActionResult AjaxValidationProblem(string? fallbackError = null)
    {
        var errors = ModelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var summary = errors.Values.SelectMany(v => v).FirstOrDefault() ?? fallbackError ?? "";
        return BadRequest(new { error = summary, errors });
    }

    /// <summary>
    /// Returns a 400 payload for a failed facade mutation during an AJAX request.
    /// </summary>
    protected IActionResult AjaxFailure(ApiResult result, string fallbackError)
    {
        if (result.ValidationErrors is { Count: > 0 })
        {
            return BadRequest(new { error = result.Error ?? fallbackError, errors = result.ValidationErrors });
        }

        return BadRequest(new { error = string.IsNullOrWhiteSpace(result.Error) ? fallbackError : result.Error, errors = (object?)null });
    }
}
