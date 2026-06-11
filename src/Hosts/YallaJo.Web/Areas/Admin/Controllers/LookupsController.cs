using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Admin.Facades;
using YallaJo.Web.Areas.Admin.Models.Lookups;
using YallaJo.Web.Infrastructure.Authorization;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Admin.Controllers;

/// <summary>
/// JSON typeahead proxies for admin lookup comboboxes (F10). The browser only ever
/// calls these MVC actions (JS5) — they relay to module suggest endpoints and return
/// the normalized {items:[{id,name}]} shape consumed by admin-lookup.js.
/// </summary>
[Area("Admin")]
[Authorize]
public sealed class LookupsController : BaseController
{
    private const int MinQueryLength = 2;

    private readonly LookupsFacade _facade;

    public LookupsController(LookupsFacade facade) => _facade = facade;

    [HttpGet("admin/lookups/users")]
    [RequirePermission(WebPermission.User.Read)]
    public async Task<IActionResult> Users(string? q, CancellationToken ct)
    {
        // C2: lookup responses are per-keystroke and permission-gated — never cache.
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < MinQueryLength)
        {
            return Json(new { items = Array.Empty<LookupItemVm>() });
        }

        var result = await _facade.SuggestUsersAsync(q.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        IReadOnlyList<LookupItemVm> items = result is { IsSuccess: true, Data: not null } ? result.Data : [];
        return Json(new { items });
    }

    [HttpGet("admin/lookups/places")]
    [RequirePermission(WebPermission.Place.Read)]
    public async Task<IActionResult> Places(string? q, CancellationToken ct)
    {
        // C2: lookup responses are per-keystroke and permission-gated — never cache.
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < MinQueryLength)
        {
            return Json(new { items = Array.Empty<LookupItemVm>() });
        }

        var result = await _facade.SuggestPlacesAsync(q.Trim(), ct);
        if (GuardSignOut(result) is { } signOut)
        {
            return signOut;
        }

        IReadOnlyList<LookupItemVm> items = result is { IsSuccess: true, Data: not null } ? result.Data : [];
        return Json(new { items });
    }
}
