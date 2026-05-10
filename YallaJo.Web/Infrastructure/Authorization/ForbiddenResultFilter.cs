using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace YallaJo.Web.Infrastructure.Authorization;

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

    private static IActionResult BuildHtmlResult(ResultExecutingContext context)
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        return new ViewResult
        {
            ViewName   = "AccessDenied",
            StatusCode = StatusCodes.Status403Forbidden,
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
