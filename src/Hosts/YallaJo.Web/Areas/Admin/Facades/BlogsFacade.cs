using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Blogs;
using YallaJo.Web.Areas.Admin.Models.Places;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// Composes the admin blog screens over the existing ContentBlogs API.
/// <para>
/// There is no "list all statuses" admin endpoint, so the list is assembled from
/// the three available endpoints per tab (Published / Queue / Deleted). Mutating
/// actions need the current optimistic-concurrency RowVersion, which the list rows
/// for the Published tab do not carry — so the facade fetches it via
/// <c>GET /api/v1/blogs/admin/{id}</c> immediately before each mutation.
/// </para>
/// </summary>
public sealed class BlogsFacade
{
    private readonly BlogsApiClient _api;
    private readonly PlacesApiClient _places;
    private readonly IOutputCacheStore _cache;

    public BlogsFacade(BlogsApiClient api, PlacesApiClient places, IOutputCacheStore cache)
    {
        _api = api;
        _places = places;
        _cache = cache;
    }

    // Place lookup feeds the related-place name dropdown (F10: never a raw GUID textbox).
    // The /api/v1/places admin list is unbounded for our purposes; cap generously.
    private const int PlaceLookupPageSize = 200;

    /// <summary>
    /// Loads selectable places (name shown, id submitted) for the related-place dropdown.
    /// Tolerant: a lookup failure yields an empty list (place is optional) rather than
    /// blocking the create/edit screen.
    /// </summary>
    public async Task<IReadOnlyList<PlaceOptionVm>> LoadPlaceOptionsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _places.ListAsync(1, PlaceLookupPageSize, new PlaceListFilterVm(), ct);
            if (!result.IsSuccess || result.Data is null) return [];

            return result.Data.Items
                .Select(p => new PlaceOptionVm { Id = p.Id, Name = p.Name })
                .OrderBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    // ── Create load ───────────────────────────────────────────────────────────────
    /// <summary>Builds a blank create form pre-populated with the related-place options (F10).</summary>
    public async Task<CreateBlogVm> GetForCreateAsync(CancellationToken ct = default)
        => new() { PlaceOptions = await LoadPlaceOptionsAsync(ct) };

    // ── List ─────────────────────────────────────────────────────────────────────
    public async Task<ApiResult<BlogListVm>> GetListAsync(
        BlogAdminTab tab, int page, int pageSize, string? search, bool? isFeatured,
        CancellationToken ct = default)
    {
        return tab switch
        {
            BlogAdminTab.Queue   => await GetQueueListAsync(page, pageSize, ct),
            BlogAdminTab.Drafts  => await GetDraftsListAsync(page, pageSize, ct),
            BlogAdminTab.Deleted => await GetDeletedListAsync(page, pageSize, search, ct),
            _                    => await GetPublishedListAsync(page, pageSize, search, isFeatured, ct),
        };
    }

    private async Task<ApiResult<BlogListVm>> GetPublishedListAsync(
        int page, int pageSize, string? search, bool? isFeatured, CancellationToken ct)
    {
        var result = await _api.ListPublishedAsync(page, pageSize, search, isFeatured, ct);
        if (result.IsUnauthorized) return ApiResult<BlogListVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BlogListVm>.Fail(result.StatusCode, result.Error ?? "Could not load blogs.");

        return ApiResult<BlogListVm>.Ok(ToListVm(
            BlogAdminTab.Published, result.Data, result.Data.Items.Select(BlogsMapper.ToRowVm), search, isFeatured));
    }

    private async Task<ApiResult<BlogListVm>> GetQueueListAsync(int page, int pageSize, CancellationToken ct)
    {
        var result = await _api.ListQueueAsync(page, pageSize, ct);
        if (result.IsUnauthorized) return ApiResult<BlogListVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BlogListVm>.Fail(result.StatusCode, result.Error ?? "Could not load the review queue.");

        var rows = result.Data.Items.Select(r =>
        {
            var vm = BlogsMapper.ToRowVm(r);
            return new BlogRowVm
            {
                Id = vm.Id, Title = vm.Title, Slug = vm.Slug, StatusLabel = "PendingReview",
                PublishedAt = vm.PublishedAt, ViewCount = vm.ViewCount, IsFeatured = vm.IsFeatured, PlaceId = vm.PlaceId,
            };
        });

        return ApiResult<BlogListVm>.Ok(ToListVm(BlogAdminTab.Queue, result.Data, rows, null, null));
    }

    private async Task<ApiResult<BlogListVm>> GetDraftsListAsync(int page, int pageSize, CancellationToken ct)
    {
        var result = await _api.ListDraftsAsync(page, pageSize, ct);
        if (result.IsUnauthorized) return ApiResult<BlogListVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BlogListVm>.Fail(result.StatusCode, result.Error ?? "Could not load draft blogs.");

        var rows = result.Data.Items.Select(r =>
        {
            var vm = BlogsMapper.ToRowVm(r);
            return new BlogRowVm
            {
                Id = vm.Id, Title = vm.Title, Slug = vm.Slug, StatusLabel = "Draft",
                PublishedAt = vm.PublishedAt, ViewCount = vm.ViewCount, IsFeatured = vm.IsFeatured, PlaceId = vm.PlaceId,
            };
        });

        return ApiResult<BlogListVm>.Ok(ToListVm(BlogAdminTab.Drafts, result.Data, rows, null, null));
    }

    private async Task<ApiResult<BlogListVm>> GetDeletedListAsync(
        int page, int pageSize, string? search, CancellationToken ct)
    {
        var result = await _api.ListDeletedAsync(page, pageSize, search, ct);
        if (result.IsUnauthorized) return ApiResult<BlogListVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<BlogListVm>.Fail(result.StatusCode, result.Error ?? "Could not load deleted blogs.");

        return ApiResult<BlogListVm>.Ok(ToListVm(
            BlogAdminTab.Deleted, result.Data, result.Data.Items.Select(BlogsMapper.ToRowVm), search, null));
    }

    private static BlogListVm ToListVm<T>(
        BlogAdminTab tab, PaginatedResponse<T> page, IEnumerable<BlogRowVm> rows,
        string? search, bool? isFeatured) => new()
    {
        Tab             = tab,
        Items           = rows.ToList(),
        PageNumber      = page.PageNumber,
        PageSize        = page.PageSize,
        TotalCount      = page.TotalCount,
        TotalPages      = page.TotalPages,
        HasPreviousPage = page.HasPreviousPage,
        HasNextPage     = page.HasNextPage,
        Search          = search,
        IsFeatured      = isFeatured,
    };

    // The /api/v1/tours endpoint caps pageSize at 50 (validator InclusiveBetween(1,50)).
    private const int TourLookupPageSize = 50;

    // ── Edit load ─────────────────────────────────────────────────────────────────
    public async Task<ApiResult<EditBlogVm>> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetAdminByIdAsync(id, ct);
        if (result.IsUnauthorized) return ApiResult<EditBlogVm>.ForceSignOut();
        if (result.IsNotFound) return ApiResult<EditBlogVm>.Fail(404, "Blog not found.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<EditBlogVm>.Fail(result.StatusCode, result.Error ?? "Could not load the blog.");

        // Tour-linking data is supplementary — load in parallel and tolerate failure
        // (the admin detail DTO carries no tours, so linked tours come from the public
        // blog detail, and titles are resolved from the published-tour lookup).
        var detailTask = _api.GetBlogByIdAsync(id, ct);
        var toursTask = _api.ListToursAsync(1, TourLookupPageSize, ct);
        var placesTask = LoadPlaceOptionsAsync(ct);
        await Task.WhenAll(detailTask, toursTask, placesTask);

        var detail = await detailTask;
        var tours = await toursTask;
        var placeOptions = await placesTask;

        var tourNames = tours is { IsSuccess: true, Data: { } td }
            ? td.Items.ToDictionary(t => t.Id, t => t.Name)
            : new Dictionary<Guid, string>();

        var linkedTourIds = detail is { IsSuccess: true, Data: { } dd }
            ? dd.LinkedTours.OrderBy(t => t.SortOrder).Select(t => t.TourId).ToList()
            : [];

        var linkedTours = linkedTourIds
            .Select(tid => new LinkedTourVm
            {
                TourId = tid,
                DisplayName = tourNames.TryGetValue(tid, out var name) ? name : tid.ToString(),
            })
            .ToList();

        // Offer only published tours that aren't already linked.
        var availableTours = (tours is { IsSuccess: true, Data: { } ad } ? ad.Items : [])
            .Where(t => !linkedTourIds.Contains(t.Id))
            .Select(t => new TourOptionVm { TourId = t.Id, Name = t.Name })
            .ToList();

        return ApiResult<EditBlogVm>.Ok(
            BlogsMapper.ToEditVm(result.Data, linkedTours, availableTours, placeOptions));
    }

    // ── Tour linking (Phase 4) ──────────────────────────────────────────────────────
    public Task<ApiResult> LinkTourAsync(Guid id, Guid tourId, CancellationToken ct = default)
        => WithRowVersion(
            id,
            rv => _api.LinkToursAsync(id, new BlogLinkToursRequest(rv, [new BlogLinkTourItem(tourId)]), ct),
            "Could not link the tour.",
            ct,
            $"blog:{id}");

    public Task<ApiResult> UnlinkTourAsync(Guid id, Guid tourId, CancellationToken ct = default)
        => WithRowVersion(
            id,
            rv => _api.UnlinkTourAsync(id, tourId, new BlogRowVersionRequest(rv), ct),
            "Could not unlink the tour.",
            ct,
            $"blog:{id}");

    // ── Create / Update / Delete ──────────────────────────────────────────────────
    public async Task<ApiResult<CreateBlogResponse>> CreateAsync(CreateBlogVm vm, CancellationToken ct = default)
    {
        var result = await _api.CreateAsync(BlogsMapper.ToCreateRequest(vm), ct);
        if (result.IsUnauthorized) return ApiResult<CreateBlogResponse>.ForceSignOut();
        if (result.IsValidationError) return ApiResult<CreateBlogResponse>.ValidationFail(result.StatusCode, result.ValidationErrors!);
        if (result.IsConflict) return ApiResult<CreateBlogResponse>.Fail(409, "A blog with this slug already exists.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<CreateBlogResponse>.Fail(result.StatusCode, result.Error ?? "Could not create the blog.");

        return ApiResult<CreateBlogResponse>.Ok(result.Data);
    }

    public Task<ApiResult> UpdateAsync(EditBlogVm vm, CancellationToken ct = default)
        => Normalize(_api.UpdateAsync(vm.Id, BlogsMapper.ToUpdateRequest(vm), ct), "Could not update the blog.", $"blog:{vm.Id}");

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.DeleteAsync(id, new BlogRowVersionRequest(rv), ct), "Could not delete the blog.", ct, $"blog:{id}");

    public Task<ApiResult> RestoreAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        => Normalize(_api.RestoreAsync(id, new BlogRowVersionRequest(rowVersion), ct), "Could not restore the blog.", $"blog:{id}");

    public Task<ApiResult> PublishAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.PublishAsync(id, new BlogRowVersionRequest(rv), ct), "Could not publish the blog.", ct, $"blog:{id}");

    public Task<ApiResult> UnpublishAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.UnpublishAsync(id, new BlogRowVersionRequest(rv), ct), "Could not unpublish the blog.", ct, $"blog:{id}");

    public Task<ApiResult> ArchiveAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.ArchiveAsync(id, new BlogRowVersionRequest(rv), ct), "Could not archive the blog.", ct, $"blog:{id}");

    public Task<ApiResult> FeatureAsync(Guid id, DateTime? featuredUntil, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.FeatureAsync(id, new FeatureBlogRequest(rv, featuredUntil), ct), "Could not feature the blog.", ct, $"blog:{id}", "homepage");

    public Task<ApiResult> UnfeatureAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.UnfeatureAsync(id, new BlogRowVersionRequest(rv), ct), "Could not unfeature the blog.", ct, $"blog:{id}", "homepage");


    public Task<ApiResult> ApproveAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.ApproveAsync(id, new BlogRowVersionRequest(rv), ct), "Could not approve the blog.", ct, $"blog:{id}");

    public Task<ApiResult> RejectAsync(Guid id, string reason, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.RejectAsync(id, new RejectBlogRequest(rv, reason), ct), "Could not reject the blog.", ct, $"blog:{id}");

    // ── Moderation (Phase 2B) ─────────────────────────────────────────────────────

    public Task<ApiResult> HideAsync(Guid id, string reason, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.HideAsync(id, new HideBlogRequest(rv, reason), ct), "Could not hide the blog.", ct, $"blog:{id}", "homepage");

    public Task<ApiResult> UnhideAsync(Guid id, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.UnhideAsync(id, new BlogRowVersionRequest(rv), ct), "Could not unhide the blog.", ct, $"blog:{id}", "homepage");

    public Task<ApiResult> RemoveAsync(Guid id, string reason, CancellationToken ct = default)
        => WithRowVersion(id, rv => _api.RemoveAsync(id, new RemoveBlogRequest(rv, reason), ct), "Could not remove the blog.", ct, $"blog:{id}");

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches the current RowVersion (admin detail) then invokes the mutation.
    /// Needed because Published list rows do not carry RowVersion.
    /// </summary>
    private async Task<ApiResult> WithRowVersion(
        Guid id, Func<byte[], Task<ApiResult>> mutate, string fallback, CancellationToken ct,
        params string[] evictTags)
    {
        var detail = await _api.GetAdminByIdAsync(id, ct);
        if (detail.IsUnauthorized) return ApiResult.ForceSignOut();
        if (detail.IsNotFound) return ApiResult.Fail(404, "Blog not found.");
        if (!detail.IsSuccess || detail.Data is null)
            return ApiResult.Fail(detail.StatusCode, detail.Error ?? fallback);

        return await Normalize(mutate(detail.Data.RowVersion), fallback, evictTags);
    }

    // Evicts the public output-cache tags for the mutated blog on success (§8.6 C3) so the
    // cached public blog detail / homepage reflect the change immediately. Uses
    // CancellationToken.None so eviction still runs if the admin client disconnected.
    private async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback, params string[] evictTags)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            foreach (var tag in evictTags)
                await _cache.EvictByTagAsync(tag, CancellationToken.None);
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "Blog not found.");
        if (result.IsConflict) return ApiResult.Fail(409, "This blog was modified by someone else, or the action is not allowed in its current state. Please reload and try again.");
        if (result.IsValidationError) return ApiResult.Invalid(result.ValidationErrors!);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
