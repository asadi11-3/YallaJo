using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Home;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class HomeController : BaseController
{
    private readonly HomeFacade _home;

    public HomeController(HomeFacade home) => _home = home;

    [HttpGet("")]
    [HttpGet("explore")]
    [HttpGet("home")]
    [OutputCache(PolicyName = "PublicShort", Tags = new[] { "homepage" })]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var result = await _home.GetHomeAsync(ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new HomeVm());
        }

        return View(result.Data);
    }

    [HttpGet("error")]
    [HttpGet("error/{code:int}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null, CancellationToken ct = default)
    {
        var statusCode = code ?? Response.StatusCode;
        if (statusCode < StatusCodes.Status400BadRequest)
        {
            statusCode = StatusCodes.Status500InternalServerError;
        }

        Response.StatusCode = statusCode;

        var (title, message) = statusCode switch
        {
            StatusCodes.Status403Forbidden => ("Access denied", "You do not have permission to view this page."),
            StatusCodes.Status404NotFound => ("Page not found", "The page you are looking for may have moved or no longer exists."),
            StatusCodes.Status429TooManyRequests => ("Too many requests", "Please pause for a moment, then try again."),
            StatusCodes.Status503ServiceUnavailable => ("Service unavailable", "YallaJo is temporarily unavailable. Please try again soon."),
            _ => ("Something went wrong", "We could not complete your request. Please try again later."),
        };

        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = HttpContext.TraceIdentifier;
        }

        return View(new ErrorVm
        {
            StatusCode = statusCode,
            Title = title,
            Message = message,
            CorrelationId = correlationId,
        });
    }
}
