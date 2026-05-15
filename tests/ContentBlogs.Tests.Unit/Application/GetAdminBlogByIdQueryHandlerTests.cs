using System.Reflection;
using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Queries.Blog.Dtos;
using ContentBlogs.Application.Queries.Blog.GetAdminBlogById;
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
/// Admin/Edit read query tests for <see cref="GetAdminBlogByIdQuery"/>.
///
/// <para>
/// These pin the contract that distinguishes the admin path from the public
/// <c>GetBlogByIdQuery</c>:
/// </para>
/// <list type="bullet">
///   <item>Returns Draft, Published, AND Archived blogs (the public path
///     returns NotFound for Draft and Archived).</item>
///   <item>Returns an <see cref="AdminBlogDetailDto"/> with the
///     <c>RowVersion</c> field populated so the Admin UI can round-trip it to
///     the mutating endpoints (PUT, DELETE, publish, unpublish, archive).</item>
///   <item>Soft-deleted blogs remain hidden by the global query filter — the
///     admin path does NOT use <c>IgnoreQueryFilters</c>.</item>
///   <item>Public DTOs (<see cref="BlogSummaryDto"/>, <see cref="BlogDetailDto"/>,
///     <see cref="BlogTranslationDto"/>) MUST continue to hide RowVersion.</item>
/// </list>
/// </summary>
public sealed class GetAdminBlogByIdQueryHandlerTests
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, " +
        "carved into rose-coloured sandstone cliffs in the southern Jordanian " +
        "desert. Visiting at sunrise gives you an entirely different experience " +
        "compared to mid-day crowds.";

    // ── Visibility rules ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminBlogById_ReturnsNotFound_WhenMissing()
    {
        await using var db = NewDb();
        var handler = NewHandler(db);

        var result = await handler.Handle(
            new GetAdminBlogByIdQuery(Guid.NewGuid(), "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Error!.Code.Should().Be("Blog.NotFound");
    }

    [Fact]
    public async Task GetAdminBlogById_ReturnsDraftBlog()
    {
        await using var db = NewDb();
        var draft = NewBlog("admin-draft"); // stays Draft
        db.Blogs.Add(draft);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetAdminBlogByIdQuery(draft.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(
            "admins must be able to load Draft blogs in order to edit or publish them");
        result.Value!.Id.Should().Be(draft.Id);
        result.Value.Status.Should().Be(nameof(BlogStatus.Draft));
    }

    [Fact]
    public async Task GetAdminBlogById_ReturnsPublishedBlog()
    {
        await using var db = NewDb();
        var blog = NewBlog("admin-published");
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetAdminBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(BlogStatus.Published));
        result.Value.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAdminBlogById_ReturnsArchivedBlog()
    {
        await using var db = NewDb();
        var blog = NewBlog("admin-archived");
        blog.Publish(DateTime.UtcNow);
        blog.Archive(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetAdminBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(
            "Archived blogs are still editable by admins to e.g. correct meta-data " +
            "or restore content even though public reads hide them");
        result.Value!.Status.Should().Be(nameof(BlogStatus.Archived));
    }

    [Fact]
    public async Task GetAdminBlogById_ReturnsNotFound_WhenSoftDeleted()
    {
        await using var db = NewDb();
        var blog = NewBlog("admin-deleted");
        blog.Publish(DateTime.UtcNow);
        blog.Delete(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetAdminBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound,
            "soft-deleted blogs must remain hidden — the admin path uses the same " +
            "global query filter, no IgnoreQueryFilters");
    }

    // ── RowVersion contract ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminBlogById_IncludesRowVersion()
    {
        // EF Core's InMemory provider does NOT auto-populate [Timestamp] values.
        // Set the desired token BEFORE SaveChanges so the in-memory store
        // persists exactly what we want the handler to project back into the DTO.
        await using var db = NewDb();
        var blog = NewBlog("admin-rv");
        var expectedToken = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        SetRowVersion(blog, expectedToken);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new GetAdminBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RowVersion.Should().NotBeNull();
        result.Value.RowVersion.Should().BeEquivalentTo(expectedToken,
            "the admin DTO must round-trip the EF concurrency token so the UI can " +
            "send it back to the mutation endpoints");
    }

    // ── Side-effect freedom ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminBlogById_DoesNotMutateViewCount()
    {
        await using var db = NewDb();
        var blog = NewBlog("admin-no-mutate");
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        var viewCountBefore = blog.ViewCount;

        await NewHandler(db).Handle(
            new GetAdminBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.ViewCount.Should().Be(viewCountBefore,
            "an admin read MUST NOT increment view counts — that is the dedicated " +
            "POST /views endpoint's job");
    }

    [Fact]
    public async Task GetAdminBlogById_DoesNotCreateTranslations()
    {
        await using var db = NewDb();
        var blog = NewBlog("admin-no-translation");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();
        var translationsBefore = await db.BlogTranslations.CountAsync();

        await NewHandler(db).Handle(
            new GetAdminBlogByIdQuery(blog.Id, "ar"), // language without translation
            CancellationToken.None);

        var translationsAfter = await db.BlogTranslations.CountAsync();
        translationsAfter.Should().Be(translationsBefore,
            "an admin read MUST NOT lazily create translation rows on language miss");
    }

    // ── Public-DTO redaction (regression) ─────────────────────────────────────

    [Fact]
    public void PublicBlogDtos_StillDoNotExpose_RowVersion()
    {
        // Hard regression guard.  If a future developer adds RowVersion to the
        // public DTOs (e.g. to "simplify" the admin flow), this test fires.
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

    [Fact]
    public void AdminBlogDetailDto_DoesExpose_RowVersion()
    {
        // Positive counterpart of the previous test: the admin DTO MUST surface
        // RowVersion because that is the whole reason the admin endpoint exists.
        typeof(AdminBlogDetailDto)
            .GetProperty("RowVersion", BindingFlags.Instance | BindingFlags.Public)
            .Should().NotBeNull(
                "AdminBlogDetailDto must expose RowVersion so the Admin UI can " +
                "round-trip it to PUT/DELETE/publish/unpublish/archive");
    }

    // ── Author-hierarchy guard ────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminBlogById_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = NewDb();
        var blog = NewBlog("admin-hierarchy");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewHandler(db, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new GetAdminBlogByIdQuery(blog.Id, "en"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Error!.Code.Should().Be("Blog.AuthorHierarchyForbidden");
        result.Value.Should().BeNull(
            "the handler must NOT return AdminBlogDetailDto (with RowVersion) " +
            "when the actor cannot manage the author's content");
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-admin-read-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static Blog NewBlog(string slug) =>
        Blog.Create(
            title:            $"Blog {slug}",
            slug:             slug,
            content:          ValidContent,
            authorId:         Guid.NewGuid(),
            sourceLanguageId: EnglishLanguageId,
            utcNow:           DateTime.UtcNow);

    private static IBlogRepository Repo(ContentBlogsDbContext db) => new BlogRepository(db);

    private static IActiveLanguageProvider Languages()
    {
        var p = Substitute.For<IActiveLanguageProvider>();
        p.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new ActiveLanguage(EnglishLanguageId, "en") });
        return p;
    }

    private static IBlogAuthorHierarchyGuard PermissiveGuard()
    {
        var guard = Substitute.For<IBlogAuthorHierarchyGuard>();
        guard.EnsureCanManageBlogOwnedByAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        return guard;
    }

    private static IBlogAuthorHierarchyGuard ForbiddenGuard()
    {
        var guard = Substitute.For<IBlogAuthorHierarchyGuard>();
        guard.EnsureCanManageBlogOwnedByAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(
                new Error(
                    "Blog.AuthorHierarchyForbidden",
                    "You cannot manage content created by a user at the same or higher privilege level."),
                Outcome.Forbidden));
        return guard;
    }

    private static GetAdminBlogByIdQueryHandler NewHandler(
        ContentBlogsDbContext db,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:         Repo(db),
            authorHierarchyGuard:   guard ?? PermissiveGuard(),
            activeLanguageProvider: Languages(),
            logger:                 NullLogger<GetAdminBlogByIdQueryHandler>.Instance);

    /// <summary>
    /// Sets <c>AuditableEntity.RowVersion</c> via reflection because EF Core's
    /// InMemory provider does not populate <c>[Timestamp]</c> values.
    /// Mirrors the helper in <c>BlogLifecycleCommandHandlerTests</c>.
    /// </summary>
    private static void SetRowVersion(Blog blog, byte[] value)
    {
        var prop = typeof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity)
            .GetProperty(
                nameof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity.RowVersion),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop!.SetValue(blog, value);
    }
}
