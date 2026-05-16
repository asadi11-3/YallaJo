using System.Reflection;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Application.Queries.Blog.GetBlogById;
using ContentBlogs.Application.Queries.Blog.GetBlogBySlug;
using ContentBlogs.Application.Queries.Blog.ListBlogs;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Phase-1 read-query handler tests covering visibility rules, translation
/// fallback, cache-key wiring, and public-DTO safety.  Uses EF Core InMemory
/// against a real <see cref="ContentBlogsDbContext"/> with the real query
/// filters applied.
/// </summary>
public sealed class BlogReadQueryTests
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private static readonly Guid ArabicLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, " +
        "carved into rose-coloured sandstone cliffs in the southern Jordanian " +
        "desert. Visiting at sunrise gives you an entirely different experience " +
        "compared to mid-day crowds.";

    // ── ListBlogs ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBlogs_ReturnsOnlyPublishedBlogs()
    {
        await using var db = NewDb();
        var published = NewBlog("published-one");
        published.Publish(DateTime.UtcNow);
        var draft = NewBlog("draft-one"); // remains Draft

        db.Blogs.AddRange(published, draft);
        await db.SaveChangesAsync();

        var handler = NewListHandler(db);
        var result = await handler.Handle(
            new ListBlogsQuery(Page: 1, PageSize: 20, AcceptLanguage: "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle()
            .Which.Slug.Should().Be("published-one");
    }

    [Fact]
    public async Task ListBlogs_ExcludesDraftBlogs()
    {
        await using var db = NewDb();
        var draft = NewBlog("only-draft");

        db.Blogs.Add(draft);
        await db.SaveChangesAsync();

        var result = await NewListHandler(db).Handle(
            new ListBlogsQuery(Page: 1, PageSize: 20, AcceptLanguage: "en"),
            CancellationToken.None);

        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListBlogs_ExcludesArchivedBlogs()
    {
        await using var db = NewDb();
        var archived = NewBlog("archived-one");
        archived.Publish(DateTime.UtcNow);
        archived.Archive(DateTime.UtcNow.AddMinutes(1));

        db.Blogs.Add(archived);
        await db.SaveChangesAsync();

        var result = await NewListHandler(db).Handle(
            new ListBlogsQuery(Page: 1, PageSize: 20, AcceptLanguage: "en"),
            CancellationToken.None);

        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ListBlogs_ExcludesSoftDeletedBlogs()
    {
        await using var db = NewDb();
        var published = NewBlog("published-then-deleted");
        published.Publish(DateTime.UtcNow);
        published.Delete(DateTime.UtcNow.AddMinutes(1));

        db.Blogs.Add(published);
        await db.SaveChangesAsync();

        var result = await NewListHandler(db).Handle(
            new ListBlogsQuery(Page: 1, PageSize: 20, AcceptLanguage: "en"),
            CancellationToken.None);

        result.Value!.Items.Should().BeEmpty(
            "the global query filter must hide soft-deleted blogs from public lists");
    }

    [Fact]
    public async Task ListBlogs_UsesRequestedTranslation_WhenAvailable()
    {
        await using var db = NewDb();
        var blog = NewBlog("petra-en-base");
        blog.AddTranslation(BlogTranslation.Create(
            blogId:     blog.Id,
            languageId: ArabicLanguageId,
            title:      "بترا عند الفجر",
            content:    new string('ك', 200),
            summary:    "ملخص عربي"));
        blog.Publish(DateTime.UtcNow);

        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewListHandler(db).Handle(
            new ListBlogsQuery(AcceptLanguage: "ar"),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle().Subject
            .Title.Should().Be("بترا عند الفجر");
        result.Value.Items[0].LanguageCode.Should().Be("ar");
    }

    [Fact]
    public async Task ListBlogs_FallsBackToBaseFields_WhenTranslationMissing()
    {
        await using var db = NewDb();
        var blog = NewBlog("fallback-blog");
        blog.Publish(DateTime.UtcNow);

        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewListHandler(db).Handle(
            new ListBlogsQuery(AcceptLanguage: "ar"),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle().Subject
            .Title.Should().Be(blog.Title, "no Arabic translation exists → fall back to base title");
    }

    [Fact]
    public async Task ListBlogs_FiltersByPlaceId_WhenProvided()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var placeB = Guid.NewGuid();

        var blogA = NewBlog("blog-place-a", placeId: placeA);
        blogA.Publish(DateTime.UtcNow);
        var blogB = NewBlog("blog-place-b", placeId: placeB);
        blogB.Publish(DateTime.UtcNow);

        db.Blogs.AddRange(blogA, blogB);
        await db.SaveChangesAsync();

        var result = await NewListHandler(db).Handle(
            new ListBlogsQuery(PlaceId: placeA, AcceptLanguage: "en"),
            CancellationToken.None);

        result.Value!.Items.Should().ContainSingle().Subject.Slug.Should().Be("blog-place-a");
    }

    // ── GetBlogById ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBlogById_ReturnsNotFound_WhenMissing()
    {
        await using var db = NewDb();
        var handler = NewGetByIdHandler(db);

        var result = await handler.Handle(
            new GetBlogByIdQuery(Guid.NewGuid(), "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Error!.Code.Should().Be("Blog.NotFound");
    }

    [Fact]
    public async Task GetBlogById_ReturnsNotFound_WhenDraft()
    {
        await using var db = NewDb();
        var draft = NewBlog("draft-blog"); // unpublished

        db.Blogs.Add(draft);
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(draft.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound,
            "Draft blogs must not leak existence to anonymous callers");
    }

    [Fact]
    public async Task GetBlogById_ReturnsNotFound_WhenArchived()
    {
        await using var db = NewDb();
        var archived = NewBlog("archived-blog");
        archived.Publish(DateTime.UtcNow);
        archived.Archive(DateTime.UtcNow.AddMinutes(1));

        db.Blogs.Add(archived);
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(archived.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task GetBlogById_ReturnsDetail_WhenPublished()
    {
        await using var db = NewDb();
        var blog = NewBlog("visible-blog");
        blog.Publish(DateTime.UtcNow);

        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(blog.Id);
        result.Value.Slug.Should().Be("visible-blog");
    }

    [Fact]
    public async Task GetBlogById_UsesRequestedTranslation_WhenAvailable()
    {
        await using var db = NewDb();
        var blog = NewBlog("with-ar-translation");
        blog.AddTranslation(BlogTranslation.Create(
            blogId:     blog.Id,
            languageId: ArabicLanguageId,
            title:      "العنوان العربي",
            content:    new string('ك', 200),
            summary:    "ملخص"));
        blog.Publish(DateTime.UtcNow);

        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(blog.Id, "ar"),
            CancellationToken.None);

        result.Value!.Title.Should().Be("العنوان العربي");
        result.Value.LanguageCode.Should().Be("ar");
    }

    [Fact]
    public async Task GetBlogById_FallsBackToBaseFields_WhenTranslationMissing()
    {
        await using var db = NewDb();
        var blog = NewBlog("no-ar-translation");
        blog.Publish(DateTime.UtcNow);

        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(blog.Id, "ar"),
            CancellationToken.None);

        result.Value!.Title.Should().Be(blog.Title);
        result.Value.LanguageCode.Should().Be("default",
            "no translation exists for Arabic → resolved language code is 'default'");
    }

    // ── GetBlogBySlug ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBlogBySlug_NormalizesSlug()
    {
        await using var db = NewDb();
        var blog = NewBlog("petra-guide");
        blog.Publish(DateTime.UtcNow);

        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewGetBySlugHandler(db).Handle(
            new GetBlogBySlugQuery("  PETRA-GUIDE  ", "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be("petra-guide");
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsNotFound_WhenMissing()
    {
        await using var db = NewDb();

        var result = await NewGetBySlugHandler(db).Handle(
            new GetBlogBySlugQuery("no-such-slug", "en"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsNotFound_WhenDraft()
    {
        await using var db = NewDb();
        var draft = NewBlog("hidden-draft");

        db.Blogs.Add(draft);
        await db.SaveChangesAsync();

        var result = await NewGetBySlugHandler(db).Handle(
            new GetBlogBySlugQuery("hidden-draft", "en"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsDetail_WhenPublished()
    {
        await using var db = NewDb();
        var blog = NewBlog("public-blog");
        blog.Publish(DateTime.UtcNow);

        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewGetBySlugHandler(db).Handle(
            new GetBlogBySlugQuery("public-blog", "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be("public-blog");
    }

    // ── Linked-tours summary (read-model) ─────────────────────────────────────

    [Fact]
    public async Task GetBlogById_ReturnsTourSummary_WhenBlogHasLinkedTours()
    {
        await using var db = NewDb();
        var blog = NewBlog("with-tours");
        blog.Publish(DateTime.UtcNow);

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        db.Blogs.Add(blog);
        db.BlogTours.Add(BlogTour.Create(blog.Id, t1, 0));
        db.BlogTours.Add(BlogTour.Create(blog.Id, t2, 1));
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TourCount.Should().Be(2);
        result.Value.LinkedTours.Should().HaveCount(2);
        result.Value.LinkedTours.Select(lt => lt.TourId)
            .Should().BeEquivalentTo(new[] { t1, t2 });
    }

    [Fact]
    public async Task GetBlogById_ReturnsEmptyTourSummary_WhenNoLinkedTours()
    {
        await using var db = NewDb();
        var blog = NewBlog("no-tours");
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TourCount.Should().Be(0);
        result.Value.LinkedTours.Should().NotBeNull("expected an empty array, never null");
        result.Value.LinkedTours.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBlogById_OrdersLinkedTours_BySortOrderThenTourId()
    {
        await using var db = NewDb();
        var blog = NewBlog("ordered-tours");
        blog.Publish(DateTime.UtcNow);

        // Deterministic Guids so the TourId-tiebreak ordering is observable.
        var tourA = new Guid("00000000-0000-0000-0000-0000000000A1");
        var tourB = new Guid("00000000-0000-0000-0000-0000000000B2");
        var tourC = new Guid("00000000-0000-0000-0000-0000000000C3");

        db.Blogs.Add(blog);
        // Insertion order is intentionally NOT the expected output order.
        db.BlogTours.Add(BlogTour.Create(blog.Id, tourC, 5)); // SortOrder=5
        db.BlogTours.Add(BlogTour.Create(blog.Id, tourB, 2)); // SortOrder=2
        db.BlogTours.Add(BlogTour.Create(blog.Id, tourA, 2)); // SortOrder=2  (tiebreaker by TourId)
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Expected: (tourA,2), (tourB,2), (tourC,5) — TourA precedes TourB at
        // SortOrder=2 because A < B as Guids; both precede TourC at SortOrder=5.
        result.Value!.LinkedTours.Select(lt => lt.TourId)
            .Should().Equal(new[] { tourA, tourB, tourC });
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsSameTourSummary_AsGetById()
    {
        await using var db = NewDb();
        var blog = NewBlog("same-summary");
        blog.Publish(DateTime.UtcNow);

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        db.Blogs.Add(blog);
        db.BlogTours.Add(BlogTour.Create(blog.Id, t1, 0));
        db.BlogTours.Add(BlogTour.Create(blog.Id, t2, 1));
        await db.SaveChangesAsync();

        var byId = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);
        var bySlug = await NewGetBySlugHandler(db).Handle(
            new GetBlogBySlugQuery(blog.Slug, "en"),
            CancellationToken.None);

        byId.IsSuccess.Should().BeTrue();
        bySlug.IsSuccess.Should().BeTrue();

        bySlug.Value!.TourCount.Should().Be(byId.Value!.TourCount);
        bySlug.Value.LinkedTours.Should().BeEquivalentTo(byId.Value.LinkedTours,
            options => options.WithStrictOrdering());
    }

    // ── Visibility regression: tour summary never leaks the blog ─────────────

    [Fact]
    public async Task GetBlogById_ReturnsNotFound_ForDraftEvenIfHasLinkedTours()
    {
        await using var db = NewDb();
        var draft = NewBlog("draft-with-tours"); // remains Draft

        db.Blogs.Add(draft);
        db.BlogTours.Add(BlogTour.Create(draft.Id, Guid.NewGuid(), 0));
        await db.SaveChangesAsync();

        var result = await NewGetByIdHandler(db).Handle(
            new GetBlogByIdQuery(draft.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound,
            "Draft blogs must remain invisible to anonymous callers regardless of linked-tour data");
        result.Value.Should().BeNull("no DTO body should be returned for not-visible blogs");
    }

    [Fact]
    public async Task GetBlogBySlug_ReturnsNotFound_ForDraftEvenIfHasLinkedTours()
    {
        await using var db = NewDb();
        var draft = NewBlog("draft-slug-with-tours");

        db.Blogs.Add(draft);
        db.BlogTours.Add(BlogTour.Create(draft.Id, Guid.NewGuid(), 0));
        await db.SaveChangesAsync();

        var result = await NewGetBySlugHandler(db).Handle(
            new GetBlogBySlugQuery(draft.Slug, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Value.Should().BeNull();
    }

    // ── Cache key wiring ──────────────────────────────────────────────────────

    [Fact]
    public void ListBlogsQuery_UsesContentBlogsCacheKeys()
    {
        var q = new ListBlogsQuery(Page: 2, PageSize: 10, AcceptLanguage: "en");

        q.CacheKey.Should().StartWith("cb:blogs:");
        q.Tags.Should().Contain(ContentBlogsCacheKeys.BlogsListTag);
    }

    [Fact]
    public void ListBlogsQuery_WithPlaceId_AddsBlogPlaceTag()
    {
        var placeId = Guid.NewGuid();
        var q = new ListBlogsQuery(PlaceId: placeId);

        q.Tags.Should().Contain(ContentBlogsCacheKeys.BlogPlaceTag(placeId));
    }

    [Fact]
    public void GetBlogByIdQuery_UsesContentBlogsCacheKeys()
    {
        var id = Guid.NewGuid();
        var q = new GetBlogByIdQuery(id, "en");

        q.CacheKey.Should().Be(ContentBlogsCacheKeys.Blog(id, "en"));
        q.Tags.Should().ContainSingle()
            .Which.Should().Be(ContentBlogsCacheKeys.BlogTag(id));
    }

    [Fact]
    public void GetBlogBySlugQuery_UsesContentBlogsCacheKeys()
    {
        var q = new GetBlogBySlugQuery("petra-guide", "en");

        q.CacheKey.Should().Be(ContentBlogsCacheKeys.BlogBySlug("petra-guide", "en"));
        q.Tags.Should().Contain(ContentBlogsCacheKeys.BlogSlugTag("petra-guide"));
    }

    [Fact]
    public void GetBlogBySlugQuery_CacheKey_NormalizesSlug()
    {
        var canonical = new GetBlogBySlugQuery("petra-guide", "en");
        var messy = new GetBlogBySlugQuery("  PETRA-GUIDE  ", "EN");

        // Slug normalization → same key.  Accept-language normalization → same key.
        messy.CacheKey.Should().Be(canonical.CacheKey);
    }

    [Fact]
    public void ListBlogsQuery_ImplementsICacheableQuery()
    {
        typeof(ICacheableQuery).IsAssignableFrom(typeof(ListBlogsQuery)).Should().BeTrue();
        typeof(ICacheableQuery).IsAssignableFrom(typeof(GetBlogByIdQuery)).Should().BeTrue();
        typeof(ICacheableQuery).IsAssignableFrom(typeof(GetBlogBySlugQuery)).Should().BeTrue();
    }

    // ── DTO safety ────────────────────────────────────────────────────────────

    [Fact]
    public void PublicBlogDtos_DoNotExpose_RowVersion_Or_AuthorId()
    {
        var summaryProps = typeof(BlogSummaryDto).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
        var detailProps = typeof(BlogDetailDto).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
        var translationProps = typeof(BlogTranslationDto).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();
        var tourSummaryProps = typeof(BlogTourSummaryDto).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();

        var forbiddenNames = new[]
        {
            "RowVersion",
            "AuthorId",
            "CreatedByUserId",
            "IsDeleted",
            "DeletedAt",
        };

        foreach (var prop in forbiddenNames)
        {
            summaryProps.Should().NotContain(prop,
                $"BlogSummaryDto must not expose '{prop}'");
            detailProps.Should().NotContain(prop,
                $"BlogDetailDto must not expose '{prop}'");
            translationProps.Should().NotContain(prop,
                $"BlogTranslationDto must not expose '{prop}'");
            tourSummaryProps.Should().NotContain(prop,
                $"BlogTourSummaryDto must not expose '{prop}'");
        }
    }

    [Fact]
    public void BlogTourSummaryDto_ExposesOnly_TourIdAndSortOrder()
    {
        // Positive lock — any new property added to BlogTourSummaryDto would
        // require both an explicit test update and a Tour-metadata coupling
        // review (we MUST NOT leak Tour name/slug/status from ContentBlogs).
        var props = typeof(BlogTourSummaryDto)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToList();

        props.Should().Equal(new[] { nameof(BlogTourSummaryDto.SortOrder), nameof(BlogTourSummaryDto.TourId) });
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-read-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static Blog NewBlog(string slug, Guid? placeId = null) =>
        Blog.Create(
            title:            $"Blog {slug}",
            slug:             slug,
            content:          ValidContent,
            authorId:         Guid.NewGuid(),
            sourceLanguageId: EnglishLanguageId,
            utcNow:           DateTime.UtcNow,
            placeId:          placeId);

    private static IBlogRepository Repo(ContentBlogsDbContext db) => new BlogRepository(db);

    private static IActiveLanguageProvider Languages()
    {
        var p = Substitute.For<IActiveLanguageProvider>();
        p.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ActiveLanguage(EnglishLanguageId, "en"),
                new ActiveLanguage(ArabicLanguageId, "ar"),
            });
        return p;
    }

    private static ListBlogsQueryHandler NewListHandler(ContentBlogsDbContext db) =>
        new(Repo(db), Languages(), NullLogger<ListBlogsQueryHandler>.Instance);

    private static GetBlogByIdQueryHandler NewGetByIdHandler(ContentBlogsDbContext db) =>
        new(Repo(db), Languages(), NullLogger<GetBlogByIdQueryHandler>.Instance);

    private static GetBlogBySlugQueryHandler NewGetBySlugHandler(ContentBlogsDbContext db) =>
        new(Repo(db), Languages(), NullLogger<GetBlogBySlugQueryHandler>.Instance);
}
