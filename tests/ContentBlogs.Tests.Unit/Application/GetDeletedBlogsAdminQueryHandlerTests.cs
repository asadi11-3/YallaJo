using System.Reflection;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Application.Queries.Blog.GetBlogById;
using ContentBlogs.Application.Queries.Blog.GetDeletedBlogsAdmin;
using ContentBlogs.Application.Queries.Blog.ListBlogs;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Tests for <see cref="GetDeletedBlogsAdminQuery"/> — admin-only listing
/// of soft-deleted blogs that includes <c>RowVersion</c> so the admin UI can
/// call <c>POST /api/v1/blogs/{id}/restore</c> without reading the EF
/// concurrency token from SQL by hand.
/// </summary>
public sealed class GetDeletedBlogsAdminQueryHandlerTests
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, " +
        "carved into rose-coloured sandstone cliffs in the southern Jordanian " +
        "desert. Visiting at sunrise gives you an entirely different experience " +
        "compared to mid-day crowds.";

    [Fact]
    public async Task GetDeletedBlogsAdmin_ReturnsOnlyDeletedBlogs()
    {
        await using var db = NewDb();
        var deleted = NewDraftBlog("deleted-one");
        deleted.Delete(DateTime.UtcNow);
        var live = NewDraftBlog("live-one");
        db.Blogs.AddRange(deleted, live);
        await db.SaveChangesAsync();

        var handler = NewHandler(db);
        var result = await handler.Handle(
            new GetDeletedBlogsAdminQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle()
            .Which.Slug.Should().Be("deleted-one");
    }

    [Fact]
    public async Task GetDeletedBlogsAdmin_DoesNotReturnActiveBlogs()
    {
        await using var db = NewDb();
        var draft = NewDraftBlog("draft");
        var published = NewDraftBlog("published");
        published.Publish(DateTime.UtcNow);
        var archived = NewDraftBlog("archived");
        archived.Publish(DateTime.UtcNow);
        archived.Archive(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.AddRange(draft, published, archived);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetDeletedBlogsAdminQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty(
            "the admin-deleted listing only returns IsDeleted == true rows");
    }

    [Fact]
    public async Task GetDeletedBlogsAdmin_IncludesRowVersion()
    {
        // EF Core InMemory does NOT auto-populate [Timestamp] values, so we
        // set the desired token BEFORE SaveChanges and expect the handler to
        // surface it via the DTO.
        await using var db = NewDb();
        var blog = NewDraftBlog("with-rv");
        var expected = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
        SetRowVersion(blog, expected);
        blog.Delete(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetDeletedBlogsAdminQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Items.Single();
        item.RowVersion.Should().NotBeNullOrEmpty();
        item.RowVersion.Should().BeEquivalentTo(expected,
            "the admin-deleted DTO must round-trip the EF concurrency token " +
            "so the UI can pass it back to POST /restore");
    }

    [Fact]
    public async Task GetDeletedBlogsAdmin_FiltersByStatus()
    {
        await using var db = NewDb();
        var draftDeleted = NewDraftBlog("dd");
        draftDeleted.Delete(DateTime.UtcNow);
        var publishedDeleted = NewDraftBlog("pd");
        publishedDeleted.Publish(DateTime.UtcNow);
        publishedDeleted.Delete(DateTime.UtcNow.AddMinutes(1));
        var archivedDeleted = NewDraftBlog("ad");
        archivedDeleted.Publish(DateTime.UtcNow);
        archivedDeleted.Archive(DateTime.UtcNow.AddMinutes(1));
        archivedDeleted.Delete(DateTime.UtcNow.AddMinutes(2));
        db.Blogs.AddRange(draftDeleted, publishedDeleted, archivedDeleted);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetDeletedBlogsAdminQuery(Status: BlogStatus.Published),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle()
            .Which.Slug.Should().Be("pd");
    }

    [Fact]
    public async Task GetDeletedBlogsAdmin_FiltersByPlaceId()
    {
        await using var db = NewDb();
        var placeA = Guid.NewGuid();
        var placeB = Guid.NewGuid();
        var aDeleted = NewDraftBlog("a-deleted", placeA);
        aDeleted.Delete(DateTime.UtcNow);
        var bDeleted = NewDraftBlog("b-deleted", placeB);
        bDeleted.Delete(DateTime.UtcNow);
        db.Blogs.AddRange(aDeleted, bDeleted);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetDeletedBlogsAdminQuery(PlaceId: placeA),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle()
            .Which.Slug.Should().Be("a-deleted");
    }

    [Fact]
    public async Task GetDeletedBlogsAdmin_SearchesByTitleOrSlug()
    {
        await using var db = NewDb();
        var blog1 = NewDraftBlog("petra-sunrise");
        blog1.Delete(DateTime.UtcNow);
        var blog2 = NewDraftBlog("amman-citadel");
        blog2.Delete(DateTime.UtcNow);
        db.Blogs.AddRange(blog1, blog2);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetDeletedBlogsAdminQuery(Search: "petra"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle()
            .Which.Slug.Should().Be("petra-sunrise");
    }

    [Fact]
    public async Task GetDeletedBlogsAdmin_OrdersByDeletedAtDesc_ByDefault()
    {
        await using var db = NewDb();
        var older = NewDraftBlog("older");
        older.Delete(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = NewDraftBlog("newer");
        newer.Delete(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        db.Blogs.AddRange(older, newer);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetDeletedBlogsAdminQuery(),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var slugs = result.Value!.Items.Select(i => i.Slug).ToArray();
        slugs.Should().ContainInOrder("newer", "older")
            .And.HaveCount(2,
                "default sort is DeletedAt descending — newest deletions first");
    }

    [Fact]
    public async Task PublicReads_StillDoNotReturnDeletedBlogs()
    {
        // Regression: the admin-deleted path must NOT change public read
        // behavior — public queries continue to hide soft-deleted rows via
        // the global query filter.
        await using var db = NewDb();
        var deleted = NewDraftBlog("public-cant-see");
        deleted.Publish(DateTime.UtcNow);
        deleted.Delete(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(deleted);
        await db.SaveChangesAsync();

        // Public list query
        var listResult = await db.Blogs
            .Where(b => b.Status == BlogStatus.Published)
            .CountAsync();
        listResult.Should().Be(0,
            "public list path must not return soft-deleted blogs");

        // Public byId / bySlug paths use the same repo with the global filter;
        // a direct DbContext check is sufficient as a regression guard.
        var byIdReachable = await db.Blogs
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == deleted.Id);
        byIdReachable.Should().BeNull(
            "deleted blog must remain hidden from public reads even after the " +
            "admin-deleted listing is exposed");
    }

    [Fact]
    public async Task GetDeletedBlogsAdmin_HonorsPagination()
    {
        await using var db = NewDb();
        for (int i = 0; i < 5; i++)
        {
            var b = NewDraftBlog($"deleted-{i}");
            b.Delete(new DateTime(2026, 1, i + 1, 0, 0, 0, DateTimeKind.Utc));
            db.Blogs.Add(b);
        }
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetDeletedBlogsAdminQuery(Page: 2, PageSize: 2),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
    }

    [Fact]
    public void AdminDeletedBlogListItemDto_ExposesRowVersion()
    {
        // Positive counterpart to the public-DTO redaction rule.
        typeof(AdminDeletedBlogListItemDto)
            .GetProperty("RowVersion", BindingFlags.Instance | BindingFlags.Public)
            .Should().NotBeNull(
                "AdminDeletedBlogListItemDto must expose RowVersion so the " +
                "admin UI can round-trip it to POST /restore");
    }

    [Fact]
    public void PublicBlogDtos_StillDoNotExpose_RowVersion()
    {
        // Hard regression guard mirroring GetAdminBlogByIdQueryHandlerTests —
        // ensures the new admin-deleted DTO did not leak its RowVersion shape
        // onto public DTOs.
        var publicTypes = new[]
        {
            typeof(BlogSummaryDto),
            typeof(BlogDetailDto),
            typeof(BlogTranslationDto),
        };

        foreach (var t in publicTypes)
        {
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Select(p => p.Name)
                         .ToList();
            props.Should().NotContain("RowVersion",
                $"{t.Name} is a public read DTO and must NOT expose the EF " +
                $"concurrency token to anonymous callers");
        }
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-admin-deleted-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static Blog NewDraftBlog(string slug, Guid? placeId = null) =>
        Blog.Create(
            title:            $"Blog {slug}",
            slug:             slug,
            content:          ValidContent,
            authorId:         Guid.NewGuid(),
            sourceLanguageId: EnglishLanguageId,
            utcNow:           DateTime.UtcNow,
            placeId:          placeId);

    private static IBlogRepository Repo(ContentBlogsDbContext db) => new BlogRepository(db);

    private static GetDeletedBlogsAdminQueryHandler NewHandler(ContentBlogsDbContext db) =>
        new(
            blogRepository: Repo(db),
            logger:         NullLogger<GetDeletedBlogsAdminQueryHandler>.Instance);

    private static void SetRowVersion(Blog blog, byte[] value)
    {
        var prop = typeof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity)
            .GetProperty(
                nameof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity.RowVersion),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop!.SetValue(blog, value);
    }
}
