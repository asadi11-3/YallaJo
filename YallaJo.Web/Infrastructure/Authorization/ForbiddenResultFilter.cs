using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace YallaJo.Web.Infrastructure.Authorization;

/// <summary>
/// Global MVC result filter — the single, centralized handler for every ForbidResult in the app.
///
/// Intercepts any <see cref="ForbidResult"/> BEFORE it executes and replaces it with the
/// correct response shape based on the request type:
///
///   HTML page request  → renders Views/Shared/AccessDenied.cshtml with HTTP 403
///   AJAX / JSON / HTMX → returns a bare HTTP 403 with a minimal ProblemDetails JSON body
///
/// This means:
///   - There is exactly ONE code path that renders AccessDenied.cshtml.
///   - No controller may call View("AccessDenied") or have an AccessDeniedView() helper.
///   - No HTML is ever injected into an AJAX/JSON response.
///
/// The filter is registered as a global filter in Program.cs via MvcOptions.Filters.
/// </summary>
public sealed class ForbiddenResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        if (context.Result is ForbidResult)
        {
            context.Result = IsAjaxOrJsonRequest(context.HttpContext.Request)
                ? BuildJsonResult()
                : BuildHtmlResult(context);
        }

        await next();
    }

    // ── HTML page → AccessDenied.cshtml ───────────────────────────────────────

    private static IActionResult BuildHtmlResult(ResultExecutingContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        // ViewResult is executed by MVC's view engine using the registered view locations.
        // Views/Shared/AccessDenied.cshtml is found via the standard shared fallback.
        return new ViewResult
        {
            ViewName   = "AccessDenied",
            StatusCode = StatusCodes.Status403Forbidden,
            // ITempDataDictionaryFactory is needed because ViewResult requires it.
            TempData = context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>()
                .GetTempData(context.HttpContext),
        };
    }

    // ── AJAX / JSON / HTMX → ProblemDetails JSON ─────────────────────────────

    // Change return type from IActionResult to ObjectResult for BuildJsonResult to fix CA1859
    private static ObjectResult BuildJsonResult() =>
        new ObjectResult(new
        {
            status = StatusCodes.Status403Forbidden,
            title  = "Forbidden",
            detail = "You do not have permission to perform this action.",
        })
        {
            StatusCode   = StatusCodes.Status403Forbidden,
            ContentTypes = { "application/problem+json" },
        };

    // ── Request type detection ────────────────────────────────────────────────

    private static bool IsAjaxOrJsonRequest(HttpRequest request)
    {
        // XMLHttpRequest (jQuery AJAX, fetch with header)
        if (request.Headers.XRequestedWith == "XMLHttpRequest")
            return true;

        // HTMX requests
        if (request.Headers.ContainsKey("HX-Request"))
            return true;

        // Accept: application/json
        if (request.Headers.Accept.Any(v =>
                v?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
            return true;

        // Content-Type: application/json (API-style POST)
        if (request.ContentType?.Contains(
            "application/json",
            StringComparison.OrdinalIgnoreCase) == true)
            return true;

        return false;
    }
}
