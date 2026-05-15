using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.ArchiveBlog;
using ContentBlogs.Application.Commands.Blog.Common;
using ContentBlogs.Application.Commands.Blog.CreateBlog;
using ContentBlogs.Application.Commands.Blog.DeleteBlog;
using ContentBlogs.Application.Commands.Blog.PublishBlog;
using ContentBlogs.Application.Commands.Blog.UnpublishBlog;
using ContentBlogs.Application.Commands.Blog.UpdateBlog;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Domain.Repositories;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Translation;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Integration-style handler tests using EF Core InMemory.  Tests exercise the
/// full handler pipeline: domain methods + repository + UoW + cache invalidation.
/// </summary>
public sealed class BlogLifecycleCommandHandlerTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private static readonly Guid ArabicLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, " +
        "carved into rose-coloured sandstone cliffs in the southern Jordanian " +
        "desert. Visiting at sunrise gives you an entirely different experience " +
        "compared to mid-day crowds.";

    // ── CreateBlog ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBlog_ReturnsUnauthorized_WhenCurrentUserIdMissing()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateCreateHandler(dbContext, userId: new UserIdSentinel(null));

        var result = await handler.Handle(SampleCreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Error!.Code.Should().Be("Auth.UserIdMissing");
    }

    [Fact]
    public async Task CreateBlog_ReturnsUnprocessableEntity_WhenSourceLanguageUnknown()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateCreateHandler(dbContext);

        var result = await handler.Handle(
            SampleCreateCommand() with { SourceLanguageCode = "xx" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.UnprocessableEntity);
        result.Error!.Code.Should().Be("Blog.UnknownLanguage");
    }

    [Fact]
    public async Task CreateBlog_ReturnsConflict_WhenSlugReserved()
    {
        await using var dbContext = CreateDbContext();
        var existing = NewBlog(slug: "petra-sunrise");
        dbContext.Blogs.Add(existing);
        await dbContext.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreateCreateHandler(dbContext, cache: cache);

        var result = await handler.Handle(
            SampleCreateCommand() with { Slug = "petra-sunrise" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Error!.Code.Should().Be("Blog.SlugConflict");

        // CreateBlog_DoesNotInvalidateCache_WhenSlugConflict
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBlog_CreatesBlogAndSourceTranslation_WhenValid()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateCreateHandler(dbContext);

        var result = await handler.Handle(
            SampleCreateCommand() with { Slug = "wadi-rum-guide" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);

        var saved = await dbContext.Blogs
            .Include(b => b.BlogTranslations)
            .FirstAsync();
        saved.Slug.Should().Be("wadi-rum-guide");
        saved.AuthorId.Should().Be(TestUserId);
        saved.Status.Should().Be(BlogStatus.Draft);
        saved.BlogTranslations.Should().ContainSingle();
        saved.BlogTranslations.First().LanguageId.Should().Be(EnglishLanguageId);
    }

    [Fact]
    public async Task CreateBlog_InvalidatesBlogsListTag_AfterSuccessfulSave()
    {
        await using var dbContext = CreateDbContext();
        var cache = Substitute.For<HybridCache>();
        var handler = CreateCreateHandler(dbContext, cache: cache);

        var result = await handler.Handle(
            SampleCreateCommand() with { Slug = "petra-tips" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await cache.Received(1)
            .RemoveByTagAsync(ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBlog_GeneratesSlug_FromTitle_WhenSlugMissing()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateCreateHandler(dbContext);

        var result = await handler.Handle(
            SampleCreateCommand() with { Title = "Sunset over Jordan!", Slug = null },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be("sunset-over-jordan");
    }

    // ── Hardening: Arabic / non-ASCII content ──────────────────────────────────

    [Fact]
    public async Task CreateBlog_ArabicContent_WithExplicitAsciiSlug_Succeeds()
    {
        await using var dbContext = CreateDbContext();
        var cache = Substitute.For<HybridCache>();
        var handler = CreateCreateHandler(
            dbContext,
            languageProvider: LanguageProviderWithArabicAndEnglish(),
            cache: cache);

        const string arabicTitle = "أفضل الأماكن في الأردن";
        var arabicContent = new string('ك', 200);

        var result = await handler.Handle(
            SampleCreateCommand() with
            {
                Title = arabicTitle,
                Content = arabicContent,
                SourceLanguageCode = "ar",
                Slug = "best-places-in-jordan",
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.Slug.Should().Be("best-places-in-jordan");

        var saved = await dbContext.Blogs
            .Include(b => b.BlogTranslations)
            .FirstAsync();
        saved.Title.Should().Be(arabicTitle);
        saved.Slug.Should().Be("best-places-in-jordan");
        saved.BlogTranslations.Should().ContainSingle();
        saved.BlogTranslations.First().LanguageId.Should().Be(ArabicLanguageId);
        saved.BlogTranslations.First().Title.Should().Be(arabicTitle);
    }

    [Fact]
    public async Task CreateBlog_ArabicTitle_WithoutExplicitSlug_ReturnsSlugInvalid()
    {
        await using var dbContext = CreateDbContext();
        var cache = Substitute.For<HybridCache>();
        var handler = CreateCreateHandler(
            dbContext,
            languageProvider: LanguageProviderWithArabicAndEnglish(),
            cache: cache);

        var result = await handler.Handle(
            SampleCreateCommand() with
            {
                Title = "أفضل الأماكن",
                Content = new string('ك', 200),
                SourceLanguageCode = "ar",
                Slug = null,
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Invalid);
        result.Error!.Code.Should().Be("Blog.SlugInvalid");

        // SaveChanges must NOT have run — no blog should be persisted.
        (await dbContext.Blogs.IgnoreQueryFilters().AnyAsync()).Should().BeFalse();

        // Cache must NOT have been invalidated on a failure path.
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void BlogSlugGenerator_LongTitle_TruncatesAtMaxLengthWithoutTrailingDash()
    {
        // A 300-char title made of ASCII letter + space pairs deterministically
        // slugifies to a string longer than the validator's MaxSlugLength (200).
        var longTitle = string.Join(' ', Enumerable.Repeat("word", 100));

        var slug = BlogSlugGenerator.Generate(explicitSlug: null, title: longTitle);

        slug.Length.Should().BeLessThanOrEqualTo(BlogSlugGenerator.MaxSlugLength);
        slug.Should().NotEndWith("-");
        slug.Should().MatchRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$");
    }

    [Fact]
    public async Task CreateBlog_HtmlContent_ReadTimeMinutes_StripsHtmlBeforeCounting()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateCreateHandler(dbContext);

        // 250 plain words ≈ 2 minutes at 200 wpm.  Wrap each word with an HTML
        // tag pair so a raw-content word count would massively inflate.  After
        // stripping, we still see exactly 250 words → 2 minutes.
        var words = string.Join(' ', Enumerable.Repeat("word", 250));
        var html = string.Join(' ', Enumerable.Repeat("<p>word</p>", 250));

        var result = await handler.Handle(
            SampleCreateCommand() with
            {
                Title = "HTML read-time check",
                Content = html,
                Slug = "html-read-time-check",
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await dbContext.Blogs.AsNoTracking().FirstAsync();
        // 250 words / 200 wpm = 1.25 → ceil = 2.  Without stripping, raw HTML
        // would yield ~750 tokens → 4 minutes, which would be wrong.
        saved.ReadTimeMinutes.Should().Be(2, "read-time must be calculated from stripped text");

        _ = words; // silence unused-local — pinned for documentation only.
    }

    // ── UpdateBlog ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBlog_ReturnsNotFound_WhenBlogMissing()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateUpdateHandler(dbContext);

        var result = await handler.Handle(
            new UpdateBlogCommand(
                BlogId:  Guid.NewGuid(),
                Title:   "New Title",
                Slug:    "new-slug",
                Content: ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.NotFound");
    }

    [Fact]
    public async Task UpdateBlog_ReturnsConflict_WhenSlugReserved()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "original");
        var other = NewBlog(slug: "taken");
        dbContext.Blogs.AddRange(blog, other);
        await dbContext.SaveChangesAsync();

        var handler = CreateUpdateHandler(dbContext);
        var result = await handler.Handle(
            new UpdateBlogCommand(
                BlogId:  blog.Id,
                Title:   "Updated",
                Slug:    "taken",
                Content: ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.SlugConflict");
    }

    [Fact]
    public async Task UpdateBlog_UpdatesBlog_WhenValid()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "original");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var handler = CreateUpdateHandler(dbContext);
        var result = await handler.Handle(
            new UpdateBlogCommand(
                BlogId:  blog.Id,
                Title:   "Updated Title",
                Slug:    "updated-slug",
                Content: ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await dbContext.Blogs.AsNoTracking().FirstAsync();
        reloaded.Title.Should().Be("Updated Title");
        reloaded.Slug.Should().Be("updated-slug");
    }

    [Fact]
    public async Task UpdateBlog_InvalidatesOldAndNewSlugTags_WhenSlugChanged()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "original");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreateUpdateHandler(dbContext, cache: cache);

        await handler.Handle(
            new UpdateBlogCommand(
                BlogId:  blog.Id,
                Title:   blog.Title,
                Slug:    "renamed",
                Content: ValidContent),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogSlugTag("original"), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogSlugTag("renamed"), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blog.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
    }

    // ── DeleteBlog ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBlog_ReturnsNotFound_WhenMissing()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreateDeleteHandler(dbContext);

        var result = await handler.Handle(
            new DeleteBlogCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task DeleteBlog_SoftDeletes_WhenValid()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "delete-me");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var handler = CreateDeleteHandler(dbContext);
        var result = await handler.Handle(
            new DeleteBlogCommand(blog.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Soft-deleted rows are excluded by the global query filter.
        var stillVisible = await dbContext.Blogs.FirstOrDefaultAsync(b => b.Id == blog.Id);
        stillVisible.Should().BeNull();

        var ignoringFilters = await dbContext.Blogs
            .IgnoreQueryFilters()
            .FirstAsync(b => b.Id == blog.Id);
        ignoringFilters.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBlog_InvalidatesExpectedTags_AfterSave()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "delete-me");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreateDeleteHandler(dbContext, cache: cache);

        await handler.Handle(new DeleteBlogCommand(blog.Id), CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blog.Id), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.SitemapRenderedTag, Arg.Any<CancellationToken>());
    }

    // ── PublishBlog ────────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishBlog_ReturnsNotFound_WhenMissing()
    {
        await using var dbContext = CreateDbContext();
        var handler = CreatePublishHandler(dbContext);

        var result = await handler.Handle(
            new PublishBlogCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task PublishBlog_ReturnsConflict_WhenInvalidTransition()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "already-published");
        blog.Publish(DateTime.UtcNow);
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var handler = CreatePublishHandler(dbContext);
        var result = await handler.Handle(
            new PublishBlogCommand(blog.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.InvalidTransition");
    }

    [Fact]
    public async Task PublishBlog_PublishesDraft_WhenValid()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "publish-me");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var handler = CreatePublishHandler(dbContext);
        var result = await handler.Handle(
            new PublishBlogCommand(blog.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await dbContext.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(BlogStatus.Published);
        reloaded.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishBlog_InvalidatesSitemap_AfterSave()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "publish-me");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreatePublishHandler(dbContext, cache: cache);

        await handler.Handle(new PublishBlogCommand(blog.Id), CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.SitemapRenderedTag, Arg.Any<CancellationToken>());
    }

    // ── UnpublishBlog ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UnpublishBlog_ReturnsConflict_WhenInvalidTransition()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "draft");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var handler = CreateUnpublishHandler(dbContext);
        var result = await handler.Handle(
            new UnpublishBlogCommand(blog.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.InvalidTransition");
    }

    [Fact]
    public async Task UnpublishBlog_UnpublishesPublished_WhenValid()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "unpublish-me");
        blog.Publish(DateTime.UtcNow);
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var handler = CreateUnpublishHandler(dbContext);
        var result = await handler.Handle(
            new UnpublishBlogCommand(blog.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await dbContext.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(BlogStatus.Draft);
    }

    // ── ArchiveBlog ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ArchiveBlog_ReturnsConflict_WhenInvalidTransition()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "draft");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var handler = CreateArchiveHandler(dbContext);
        var result = await handler.Handle(
            new ArchiveBlogCommand(blog.Id), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.InvalidTransition");
    }

    [Fact]
    public async Task ArchiveBlog_ArchivesPublished_WhenValid()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "archive-me");
        blog.Publish(DateTime.UtcNow);
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var handler = CreateArchiveHandler(dbContext);
        var result = await handler.Handle(
            new ArchiveBlogCommand(blog.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await dbContext.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(BlogStatus.Archived);
    }

    // ── Test helpers ───────────────────────────────────────────────────────────

    private static ContentBlogsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-lifecycle-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static CreateBlogCommand SampleCreateCommand() =>
        new(
            Title:              "Petra Sunrise: A Practical Guide",
            Content:            ValidContent,
            SourceLanguageCode: "en",
            Slug:               null,
            Summary:            "Tips for visiting Petra at sunrise.",
            MetaTitle:          "Petra Sunrise Guide",
            MetaDescription:    "Plan a sunrise visit to Petra.",
            PlaceId:            null);

    private static Blog NewBlog(string slug) =>
        Blog.Create(
            title:            "Petra Guide",
            slug:             slug,
            content:          ValidContent,
            authorId:         Guid.NewGuid(),
            sourceLanguageId: EnglishLanguageId,
            utcNow:           DateTime.UtcNow);

    private static ICurrentUser CurrentUser(Guid? userId)
    {
        var stub = Substitute.For<ICurrentUser>();
        stub.UserId.Returns(userId);
        return stub;
    }

    private static IActiveLanguageProvider LanguageProviderWithEnglish()
    {
        var provider = Substitute.For<IActiveLanguageProvider>();
        provider.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new ActiveLanguage(EnglishLanguageId, "en") });
        return provider;
    }

    private static IActiveLanguageProvider LanguageProviderWithArabicAndEnglish()
    {
        var provider = Substitute.For<IActiveLanguageProvider>();
        provider.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ActiveLanguage(EnglishLanguageId, "en"),
                new ActiveLanguage(ArabicLanguageId, "ar"),
            });
        return provider;
    }

    private static IContentBlogsUnitOfWork UnitOfWork(ContentBlogsDbContext dbContext)
    {
        var uow = Substitute.For<IContentBlogsUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => dbContext.SaveChangesAsync(ci.Arg<CancellationToken>()));
        return uow;
    }

    private static IBlogRepository Repository(ContentBlogsDbContext dbContext) =>
        new BlogRepository(dbContext);

    private sealed record UserIdSentinel(Guid? Value);

    private static readonly UserIdSentinel DefaultUser = new(TestUserId);

    private static CreateBlogCommandHandler CreateCreateHandler(
        ContentBlogsDbContext dbContext,
        UserIdSentinel? userId = null,
        HybridCache? cache = null,
        IActiveLanguageProvider? languageProvider = null) =>
        new(
            blogRepository:         Repository(dbContext),
            activeLanguageProvider: languageProvider ?? LanguageProviderWithEnglish(),
            unitOfWork:             UnitOfWork(dbContext),
            cache:                  cache ?? Substitute.For<HybridCache>(),
            currentUser:            CurrentUser((userId ?? DefaultUser).Value),
            logger:                 NullLogger<CreateBlogCommandHandler>.Instance);

    private static UpdateBlogCommandHandler CreateUpdateHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null) =>
        new(
            blogRepository: Repository(dbContext),
            unitOfWork:     UnitOfWork(dbContext),
            cache:          cache ?? Substitute.For<HybridCache>(),
            logger:         NullLogger<UpdateBlogCommandHandler>.Instance);

    private static DeleteBlogCommandHandler CreateDeleteHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null) =>
        new(
            blogRepository: Repository(dbContext),
            unitOfWork:     UnitOfWork(dbContext),
            cache:          cache ?? Substitute.For<HybridCache>(),
            logger:         NullLogger<DeleteBlogCommandHandler>.Instance);

    private static PublishBlogCommandHandler CreatePublishHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null) =>
        new(
            blogRepository: Repository(dbContext),
            unitOfWork:     UnitOfWork(dbContext),
            cache:          cache ?? Substitute.For<HybridCache>(),
            logger:         NullLogger<PublishBlogCommandHandler>.Instance);

    private static UnpublishBlogCommandHandler CreateUnpublishHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null) =>
        new(
            blogRepository: Repository(dbContext),
            unitOfWork:     UnitOfWork(dbContext),
            cache:          cache ?? Substitute.For<HybridCache>(),
            logger:         NullLogger<UnpublishBlogCommandHandler>.Instance);

    private static ArchiveBlogCommandHandler CreateArchiveHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null) =>
        new(
            blogRepository: Repository(dbContext),
            unitOfWork:     UnitOfWork(dbContext),
            cache:          cache ?? Substitute.For<HybridCache>(),
            logger:         NullLogger<ArchiveBlogCommandHandler>.Instance);
}
