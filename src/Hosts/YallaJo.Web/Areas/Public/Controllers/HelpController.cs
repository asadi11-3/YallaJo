using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Help;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class HelpController : BaseController
{
    private readonly HelpFacade _help;

    public HelpController(HelpFacade help) => _help = help;

    [HttpGet("help")]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var result = await _help.GetHelpAsync(ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new HelpVm());
        }

        return View(result.Data);
    }

    [HttpGet("help/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct = default)
    {
        var result = await _help.GetFaqAsync(id, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
                return NotFound();

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }
}
