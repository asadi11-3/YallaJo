using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet("explore")]
    [HttpGet("home")]
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
}
