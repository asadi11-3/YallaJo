using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YallaJo.Web.Areas.Content.Facades;
using YallaJo.Web.Areas.Content.Models.Blogs;
using YallaJo.Web.Infrastructure.Mvc;

namespace YallaJo.Web.Areas.Content.Controllers;

[Area("Content")]
[AllowAnonymous]
public sealed class CreatorsController : BaseController
{
    private readonly BlogsFacade _facade;

    public CreatorsController(BlogsFacade facade) => _facade = facade;

    // ----- (B) Public creator profile + follow -----

    [HttpGet("content/creators/{slug}")]
    public async Task<IActionResult> Profile(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return RedirectToAction("Index", "Blogs");

        var result = await _facade.GetCreatorProfileAsync(slug, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsNotFound)
            return NotFound();

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction("Index", "Blogs");
        }

        return View(result.Data);
    }

    [HttpPost("content/creators/{slug}/follow")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(string slug, Guid profileId, CancellationToken ct = default)
    {
        var result = await _facade.FollowAsync(profileId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("You are now following this creator.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Profile), new { slug });
    }

    [HttpPost("content/creators/{slug}/unfollow")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfollow(string slug, Guid profileId, CancellationToken ct = default)
    {
        var result = await _facade.UnfollowAsync(profileId, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("You have unfollowed this creator.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(Profile), new { slug });
    }

    // ----- (C) Creator post composer (create / edit / my blogs) -----

    [HttpGet("content/creators/write")]
    [Authorize]
    public IActionResult Write() => View(new CreateBlogVm { SourceLanguageCode = "en" });

    [HttpPost("content/creators/write")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Write(CreateBlogVm form, CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
            return View(form);

        var result = await _facade.CreateBlogAsync(form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            if (!ApplyValidationErrors(result))
                SetError(result.Error);
            return View(form);
        }

        SetSuccess("Your article was created as a draft.");
        return RedirectToAction("Details", "Blogs", new { slug = result.Data.Slug });
    }

    [HttpGet("content/creators/write/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.GetEditBlogAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsNotFound)
            return NotFound();

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return RedirectToAction(nameof(MyBlogs));
        }

        return View(result.Data);
    }

    [HttpPost("content/creators/write/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditBlogVm form, CancellationToken ct = default)
    {
        form.Id = id;

        if (!ModelState.IsValid)
            return View(form);

        var result = await _facade.UpdateBlogAsync(id, form, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess)
        {
            if (!ApplyValidationErrors(result))
                SetError(result.Error);
            return View(form);
        }

        SetSuccess("Your article was updated.");
        return RedirectToAction(nameof(MyBlogs));
    }

    [HttpGet("content/creators/my-blogs")]
    [Authorize]
    public async Task<IActionResult> MyBlogs(CancellationToken ct = default)
    {
        var result = await _facade.GetMyBlogsAsync(ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (!result.IsSuccess || result.Data is null)
        {
            SetError(result.Error);
            return View(new MyBlogsVm());
        }

        return View(result.Data);
    }

    [HttpPost("content/creators/my-blogs/{id:guid}/submit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct = default)
    {
        var result = await _facade.SubmitForReviewAsync(id, ct);
        if (GuardSignOut(result) is { } signOut) return signOut;

        if (result.IsSuccess)
            SetSuccess("Your article was submitted for review.");
        else
            SetError(result.Error);

        return RedirectToAction(nameof(MyBlogs));
    }
}
