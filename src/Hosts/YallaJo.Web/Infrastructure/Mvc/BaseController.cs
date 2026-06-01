using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Infrastructure.Mvc;

/// <summary>
/// Base class for all MVC controllers in the web (BFF) layer.
/// <para>
/// Centralizes the boilerplate that was previously duplicated in every
/// controller action that consumes a facade returning <see cref="ApiResult"/> /
/// <see cref="ApiResult{T}"/>:
/// </para>
/// <list type="bullet">
///   <item>the <c>RedirectToLogin()</c> helper (was copy-pasted per controller);</item>
///   <item>the "session expired ⇒ bounce to login" guard;</item>
///   <item>TempData success/error flash-message setters;</item>
///   <item>copying API validation errors into <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary"/>.</item>
/// </list>
/// Controllers keep full ownership of routing, permissions, model binding and
/// view selection — this base only removes mechanical repetition.
/// </summary>
public abstract class BaseController : Controller
{
    // TempData keys consumed by _Layout / _Alerts partials.
    protected const string SuccessKey = "Success";
    protected const string ErrorKey   = "Error";

    /// <summary>
    /// Redirect to the Auth area login page. Single source of truth for what
    /// was previously a private helper duplicated verbatim in each controller.
    /// </summary>
    protected RedirectToActionResult RedirectToLogin() =>
        RedirectToAction("Index", "Login", new { area = "Auth" });

    /// <summary>
    /// If the API call required the user to be signed out (401), returns the
    /// login redirect; otherwise returns <c>null</c> so the caller continues.
    /// <para>Usage: <c>if (GuardSignOut(result) is { } r) return r;</c></para>
    /// </summary>
    protected IActionResult? GuardSignOut(ApiResult result) =>
        result.RequireSignOut ? RedirectToLogin() : null;

    /// <inheritdoc cref="GuardSignOut(ApiResult)"/>
    protected IActionResult? GuardSignOut<T>(ApiResult<T> result) =>
        result.RequireSignOut ? RedirectToLogin() : null;

    /// <summary>Sets the success flash message shown after the next redirect.</summary>
    protected void SetSuccess(string message) => TempData[SuccessKey] = message;

    /// <summary>Sets the error flash message shown after the next redirect.</summary>
    protected void SetError(string? message) => TempData[ErrorKey] = message;

    /// <summary>
    /// Writes a success or error flash message based on the result, using
    /// <paramref name="successMessage"/> on success and the API's own
    /// <see cref="ApiResult.Error"/> (or <paramref name="fallbackError"/>) on failure.
    /// Mirrors the ubiquitous
    /// <c>TempData[result.IsSuccess ? "Success" : "Error"] = ...</c> one-liner.
    /// </summary>
    protected void SetFlash(ApiResult result, string successMessage, string? fallbackError = null)
    {
        if (result.IsSuccess)
            SetSuccess(successMessage);
        else
            SetError(result.Error ?? fallbackError);
    }

    /// <inheritdoc cref="SetFlash(ApiResult,string,string?)"/>
    protected void SetFlash<T>(ApiResult<T> result, string successMessage, string? fallbackError = null)
    {
        if (result.IsSuccess)
            SetSuccess(successMessage);
        else
            SetError(result.Error ?? fallbackError);
    }

    /// <summary>
    /// Copies any server-side validation errors from the API result into
    /// <see cref="Controller.ModelState"/> so they re-render against the form.
    /// Returns <c>true</c> if at least one error was applied.
    /// </summary>
    protected bool ApplyValidationErrors(ApiResult result)
    {
        if (result.ValidationErrors is not { Count: > 0 }) return false;
        foreach (var (field, messages) in result.ValidationErrors)
            foreach (var message in messages)
                ModelState.AddModelError(field, message);
        return true;
    }

    /// <inheritdoc cref="ApplyValidationErrors(ApiResult)"/>
    protected bool ApplyValidationErrors<T>(ApiResult<T> result)
    {
        if (result.ValidationErrors is not { Count: > 0 }) return false;
        foreach (var (field, messages) in result.ValidationErrors)
            foreach (var message in messages)
                ModelState.AddModelError(field, message);
        return true;
    }
}
