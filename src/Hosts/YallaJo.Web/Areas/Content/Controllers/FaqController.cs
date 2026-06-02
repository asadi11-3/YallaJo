using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Content.Facades;
using YallaJo.Web.Areas.Content.Models.Faqs;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Content.Controllers;

[Area("Content")]
[AllowAnonymous]
public sealed class FaqController : BaseController
{
    private const int DefaultPageSize = 50;

    private readonly FaqsFacade _facade;

    public FaqController(FaqsFacade facade) => _facade = facade;

    // GET /content/faq
    [HttpGet]
    public async Task<IActionResult> Index(
        string? entityType = null,
        int page = 1,
        int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = DefaultPageSize;

        var result = await _facade.GetListAsync(entityType, page, pageSize, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new FaqListVm());
        }

        return View(result.Data);
    }
}
