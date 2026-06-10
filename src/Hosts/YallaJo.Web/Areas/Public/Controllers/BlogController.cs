using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Blog;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Infrastructure.Mvc;
using YallaJo.Web.Resources;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
public sealed class BlogController(BlogFacade blog, ReviewsFacade reviews, IStringLocalizer<SharedResource> localizer) : BaseController
{
    private const string TargetType = "Blog";

    [HttpGet("blog")]
    [OutputCache(PolicyName = "PublicShort", VaryByHeaderNames = new[] { "X-Requested-With" })]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await blog.GetGridAsync(page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            var fallback = new BlogsGridVm();
            return WantsAjax() ? PartialView("_BlogResults", fallback) : View(fallback);
        }

        // Phase 7: AJAX requests (listing.js) receive just the results fragment.
        return WantsAjax() ? PartialView("_BlogResults", result.Data) : View(result.Data);
    }

    [HttpGet("blog/{slug}")]
    [OutputCache(PolicyName = "PublicLong")]
    public async Task<IActionResult> Post(string slug, CancellationToken ct = default)
    {
        var result = await blog.GetPostAsync(slug, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
            {
                return NotFound();
            }

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        PublicOutputCacheTagger.AddTag(HttpContext, $"blog:{result.Data.Id}");
        return View(result.Data);
    }

    [HttpPost("blog/{id:guid}/view")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> View(Guid id, CancellationToken ct = default)
    {
        await blog.RecordViewAsync(id, ct);
        return NoContent();
    }

    [HttpPost("blog/{id:guid}/comments")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComment(Guid id, CreateBlogCommentFormVm form, CancellationToken ct = default)
    {
        var slug = SafeSlug(form.Slug);
        if (!ModelState.IsValid)
        {
            return CommentFailure(localizer["Public.Comment.FormError"], slug);
        }

        var result = await blog.CreateCommentAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            return WantsAjax()
                ? await CommentsPartialAsync(slug, ct)
                : CommentSuccess(localizer["Public.Comment.Created"], slug);
        }

        if (WantsAjax()) return BadRequest(new { error = FailureMessage(result.Error) });
        if (!ApplyValidationErrors(result)) SetError(result.Error);
        return RedirectToAction(nameof(Post), new { slug });
    }

    [HttpPost("blog/comments/{commentId:guid}/edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(Guid commentId, Guid postId, EditBlogCommentFormVm form, CancellationToken ct = default)
    {
        var slug = SafeSlug(form.Slug);
        if (!ModelState.IsValid)
        {
            return CommentFailure(localizer["Public.Comment.EditFormError"], slug);
        }

        var result = await blog.EditCommentAsync(commentId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
        {
            return WantsAjax()
                ? await CommentsPartialAsync(slug, ct)
                : CommentSuccess(localizer["Public.Comment.Updated"], slug);
        }

        if (WantsAjax()) return BadRequest(new { error = FailureMessage(result.Error) });
        if (!ApplyValidationErrors(result)) SetError(result.Error);
        return RedirectToAction(nameof(Post), new { slug });
    }

    [HttpPost("blog/comments/{commentId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(Guid commentId, Guid postId, string? rowVersion, string? slug, CancellationToken ct = default)
    {
        var result = await blog.DeleteCommentAsync(commentId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess(localizer["Public.Comment.Deleted"]);
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Post), new { slug = SafeSlug(slug) });
    }

    [HttpPost("blog/comments/{commentId:guid}/react")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactComment(Guid commentId, string? slug, CancellationToken ct = default)
    {
        var result = await blog.AddCommentReactionAsync(commentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            return result.IsSuccess
                ? await CommentsPartialAsync(SafeSlug(slug), ct)
                : BadRequest(new { error = FailureMessage(result.Error) });
        }

        if (!result.IsSuccess && !ApplyValidationErrors(result)) SetError(result.Error);
        return RedirectToAction(nameof(Post), new { slug = SafeSlug(slug) });
    }

    [HttpPost("blog/comments/{commentId:guid}/unreact")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnreactComment(Guid commentId, string? slug, CancellationToken ct = default)
    {
        var result = await blog.RemoveCommentReactionAsync(commentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            return result.IsSuccess
                ? await CommentsPartialAsync(SafeSlug(slug), ct)
                : BadRequest(new { error = FailureMessage(result.Error) });
        }

        if (!result.IsSuccess && !ApplyValidationErrors(result)) SetError(result.Error);
        return RedirectToAction(nameof(Post), new { slug = SafeSlug(slug) });
    }

    [HttpGet("creators/{slug}")]
    [OutputCache(PolicyName = "PublicLong")]
    public async Task<IActionResult> Creator(string slug, CancellationToken ct = default)
    {
        var result = await blog.GetCreatorAsync(slug, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            if (result.IsNotFound)
            {
                return NotFound();
            }

            SetError(result.Error);
            return RedirectToAction(nameof(Index));
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            var follow = await blog.GetFollowStateAsync(result.Data.Id, ct);
            if (GuardSignOut(follow) is { } signOut) return signOut;
            if (follow is { IsSuccess: true, Data: { } state }) result.Data.IsFollowing = state.IsFollowing;
        }

        return View(result.Data);
    }

    [HttpPost("creators/{profileId:guid}/follow")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FollowCreator(Guid profileId, string creatorSlug, CancellationToken ct = default)
    {
        var result = await blog.FollowCreatorAsync(profileId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            return result.IsSuccess
                ? Json(new { success = true, isFollowing = true, message = localizer["Public.Creator.Followed"].Value })
                : BadRequest(new { error = FailureMessage(result.Error) });
        }

        if (result.IsSuccess) SetSuccess(localizer["Public.Creator.Followed"]);
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Creator), new { slug = SafeSlug(creatorSlug) });
    }

    [HttpPost("creators/{profileId:guid}/unfollow")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnfollowCreator(Guid profileId, string creatorSlug, CancellationToken ct = default)
    {
        var result = await blog.UnfollowCreatorAsync(profileId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (WantsAjax())
        {
            return result.IsSuccess
                ? Json(new { success = true, isFollowing = false, message = localizer["Public.Creator.Unfollowed"].Value })
                : BadRequest(new { error = FailureMessage(result.Error) });
        }

        if (result.IsSuccess) SetSuccess(localizer["Public.Creator.Unfollowed"]);
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Creator), new { slug = SafeSlug(creatorSlug) });
    }

    [HttpPost("blog/{slug}/report")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(string slug, [Bind(Prefix = "Report")] ReportFormVm form, CancellationToken ct = default)
    {
        form.EntityType = TargetType;

        if (!ModelState.IsValid)
        {
            SetError(localizer["Public.Report.FormError"]);
            return RedirectToAction(nameof(Post), new { slug = SafeSlug(slug) });
        }

        var result = await reviews.SubmitReportAsync(form, ct);

        if (GuardSignOut(result) is { } signOut)
            return signOut;

        if (result.IsSuccess)
            SetSuccess(localizer["Public.Review.Reported"]);
        else if (!ApplyValidationErrors(result))
            SetError(result.Error);

        return RedirectToAction(nameof(Post), new { slug = SafeSlug(slug) });
    }

    private static string SafeSlug(string? slug) => string.IsNullOrWhiteSpace(slug) ? "" : slug;

    private string FailureMessage(string? error)
        => string.IsNullOrWhiteSpace(error) ? localizer["Public.Results.Error"].Value : error;

    /// <summary>
    /// Returns the refreshed comment thread for AJAX callers (Phase 5.4); the partial swap is the success feedback.
    /// </summary>
    private async Task<IActionResult> CommentsPartialAsync(string slug, CancellationToken ct)
    {
        var result = await blog.GetPostAsync(slug, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            return BadRequest(new { error = FailureMessage(result.Error) });
        }

        return PartialView("_CommentThread", result.Data);
    }

    private IActionResult CommentFailure(string message, string slug)
    {
        if (WantsAjax()) return BadRequest(new { error = message });
        SetError(message);
        return RedirectToAction(nameof(Post), new { slug });
    }

    private IActionResult CommentSuccess(string message, string slug)
    {
        SetSuccess(message);
        return RedirectToAction(nameof(Post), new { slug });
    }
}
