using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Places;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class PlacesController : BaseController
{
    private readonly PlacesFacade _places;

    public PlacesController(PlacesFacade places) => _places = places;

    // ── GET /places ───────────────────────────────────────────────────────────
    [HttpGet("places")]
    public async Task<IActionResult> Index(
        string? city = null,
        string? country = null,
        int? ratingMin = null,
        bool hasActiveTours = false,
        int page = 1,
        CancellationToken ct = default)
    {
        var filters = new PlaceFiltersVm
        {
            City           = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Country        = string.IsNullOrWhiteSpace(country) ? null : country.Trim(),
            RatingMin      = ratingMin,
            HasActiveTours = hasActiveTours,
        };

        var result = await _places.GetGridAsync(filters, page, ct);

        if (!result.IsSuccess || result.Data is null)
        {
            // Tolerant: render a friendly empty grid (with the echoed filters
            // preserved) plus the error message, rather than a 500.
            SetError(result.Error);
            return View(new PlacesGridVm { Filters = filters });
        }

        return View(result.Data);
    }

    // ── GET /places/{slug} ──────────────────────────────────────────────────────
    [HttpGet("places/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken ct = default)
    {
        var result = await _places.GetDetailAsync(slug, ct);

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
