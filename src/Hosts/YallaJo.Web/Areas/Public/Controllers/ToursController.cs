using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
[AllowAnonymous]
public sealed class ToursController : BaseController
{
    private readonly ToursFacade _tours;

    public ToursController(ToursFacade tours) => _tours = tours;

    [HttpGet("tours")]
    public async Task<IActionResult> Index(int page = 1, string? sort = null, string? q = null, CancellationToken ct = default)
    {
        var result = await _tours.GetGridAsync(page, sort, q, ct);

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new TourGridVm { Sort = ToursFacade.NormalizeSort(sort), Query = q });
        }

        return View(result.Data);
    }

    [HttpGet("tours/{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct = default)
    {
        var result = await _tours.GetDetailAsync(slug, ct);

        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
                return NotFound();

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("tours/{slug}/join")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestJoin(string slug, Guid tourBookingId, Guid availabilitySlotId, int participantCount, string? message, CancellationToken ct = default)
    {
        var result = await _tours.SubmitJoinRequestAsync(
            new SubmitJoinRequestBody(tourBookingId, availabilitySlotId, participantCount, message), ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess("Your request to join has been sent.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Detail), new { slug });
    }
}
