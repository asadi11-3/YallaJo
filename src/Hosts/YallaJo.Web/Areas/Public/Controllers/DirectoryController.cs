using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class DirectoryController : BaseController
{
    private readonly DirectoryFacade _directory;

    public DirectoryController(DirectoryFacade directory) => _directory = directory;

    [HttpGet("directory")]
    public async Task<IActionResult> Index(
        string? q = null, string? businessType = null, string? city = null, int page = 1, CancellationToken ct = default)
    {
        var result = await _directory.GetDirectoryAsync(q, businessType, city, page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new DirectoryVm
            {
                BusinessTypes = DirectoryFacade.BusinessTypeOptions,
                Query = q,
                SelectedBusinessType = DirectoryFacade.NormalizeBusinessType(businessType),
                City = city,
            });
        }

        return View(result.Data);
    }

    [HttpGet("directory/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct = default)
    {
        var result = await _directory.GetDetailAsync(id, ct);
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
