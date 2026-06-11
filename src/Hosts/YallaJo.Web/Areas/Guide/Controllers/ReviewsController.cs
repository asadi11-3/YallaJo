using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Guide.Facades;
using YallaJo.Web.Areas.Guide.Models.Reviews;

namespace YallaJo.Web.Areas.Guide.Controllers;

public sealed class ReviewsController : GuideBaseController
{
    private const int DefaultPageSize = 20;

    private readonly GuideReviewsFacade _reviews;

    public ReviewsController(GuideReviewsFacade reviews) => _reviews = reviews;

    [HttpGet("guide/reviews")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        SetNav("Reviews");

        var result = await _reviews.GetAsync(page, DefaultPageSize, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new ReviewsVm());
        }

        return View(result.Data);
    }
}
