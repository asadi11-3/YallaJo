using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Public.Caching;
using YallaJo.Web.Areas.Public.Facades;
using YallaJo.Web.Areas.Public.Models.Blog;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Public.Controllers;

[Area("Public")]
public sealed class BlogController(BlogFacade blog) : BaseController
{
    [HttpGet("blog")]
    [OutputCache(PolicyName = "PublicShort")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await blog.GetGridAsync(page, ct);
        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new BlogsGridVm());
        }

        return View(result.Data);
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
            SetError("Please write a comment before posting.");
            return RedirectToAction(nameof(Post), new { slug });
        }

        var result = await blog.CreateCommentAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your comment was posted.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

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
            SetError("Please update the comment text before saving.");
            return RedirectToAction(nameof(Post), new { slug });
        }

        var result = await blog.EditCommentAsync(commentId, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your comment was updated.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Post), new { slug });
    }

    [HttpPost("blog/comments/{commentId:guid}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(Guid commentId, Guid postId, string? rowVersion, string? slug, CancellationToken ct = default)
    {
        var result = await blog.DeleteCommentAsync(commentId, rowVersion, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (result.IsSuccess) SetSuccess("Your comment was deleted.");
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
        if (!result.IsSuccess && !ApplyValidationErrors(result)) SetError(result.Error);

        return WantsNoContent() ? NoContent() : RedirectToAction(nameof(Post), new { slug = SafeSlug(slug) });
    }

    [HttpPost("blog/comments/{commentId:guid}/unreact")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnreactComment(Guid commentId, string? slug, CancellationToken ct = default)
    {
        var result = await blog.RemoveCommentReactionAsync(commentId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;
        if (!result.IsSuccess && !ApplyValidationErrors(result)) SetError(result.Error);

        return WantsNoContent() ? NoContent() : RedirectToAction(nameof(Post), new { slug = SafeSlug(slug) });
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
        if (result.IsSuccess) SetSuccess("You are now following this creator.");
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
        if (result.IsSuccess) SetSuccess("You are no longer following this creator.");
        else if (!ApplyValidationErrors(result)) SetError(result.Error);

        return RedirectToAction(nameof(Creator), new { slug = SafeSlug(creatorSlug) });
    }

    private static string SafeSlug(string? slug) => string.IsNullOrWhiteSpace(slug) ? "" : slug;

    private bool WantsNoContent()
        => string.Equals(Request.Headers.Accept.ToString(), "application/json", StringComparison.OrdinalIgnoreCase)
           || string.Equals(Request.Headers["X-Requested-With"].ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
}
