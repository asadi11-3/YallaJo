using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.ArchiveBlog;
using ContentBlogs.Application.Commands.Blog.Common;
using ContentBlogs.Application.Commands.Blog.CreateBlog;
using ContentBlogs.Application.Commands.Blog.DeleteBlog;
using ContentBlogs.Application.Commands.Blog.PublishBlog;
using ContentBlogs.Application.Commands.Blog.SubmitBlogForReview;
using ContentBlogs.Application.Commands.Blog.UnpublishBlog;
using ContentBlogs.Application.Commands.Blog.UpdateBlog;
using ContentBlogs.Domain.Entities.Creators;
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

    // A deterministic non-empty token used wherever the test needs *some*
    // RowVersion value but the specific bytes do not matter (e.g. the blog
    // does not exist so the handler returns NotFound before comparing).
    private static readonly byte[] SomeRowVersion = [0x01, 0x02, 0x03, 0x04];

    // Distinct from SomeRowVersion so RowVersionUtil.Equal returns false —
    // used to drive the optimistic-concurrency mismatch path.
    private static readonly byte[] StaleRowVersion = [0xDE, 0xAD, 0xBE, 0xEF];

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
        saved.AuthoredByCreatorId.Should().BeNull(
            "non-creator authors must not populate AuthoredByCreatorId");
        saved.BlogTranslations.Should().ContainSingle();
        saved.BlogTranslations.First().LanguageId.Should().Be(EnglishLanguageId);
    }

    [Fact]
    public async Task CreateBlog_PopulatesAuthoredByCreatorId_WhenAuthorHasActiveCreatorProfile()
    {
        await using var dbContext = CreateDbContext();

        var creatorProfile = SeedActiveCreatorProfile(dbContext, TestUserId);
        await dbContext.SaveChangesAsync();

        var handler = CreateCreateHandler(
            dbContext,
            creatorProfileRepository: new CreatorProfileRepository(dbContext));

        var result = await handler.Handle(
            SampleCreateCommand() with { Slug = "creator-wadi-rum-guide" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);

        var saved = await dbContext.Blogs
            .FirstAsync(b => b.Slug == "creator-wadi-rum-guide");
        saved.AuthorId.Should().Be(TestUserId,
            "AuthorId must remain the user id, not the creator profile id");
        saved.AuthoredByCreatorId.Should().Be(creatorProfile.Id,
            "creator-authored articles must reference the active CreatorProfile.Id");
        saved.Status.Should().Be(BlogStatus.Draft);
    }

    [Fact]
    public async Task SubmitForReview_Succeeds_ForCreatorAuthoredDraft_WithMatchingRowVersion()
    {
        await using var dbContext = CreateDbContext();

        var creatorProfile = SeedActiveCreatorProfile(dbContext, TestUserId);
        await dbContext.SaveChangesAsync();

        var createHandler = CreateCreateHandler(
            dbContext,
            creatorProfileRepository: new CreatorProfileRepository(dbContext));

        var createResult = await createHandler.Handle(
            SampleCreateCommand() with { Slug = "creator-petra-submit" },
            CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var draft = await dbContext.Blogs.FirstAsync(b => b.Slug == "creator-petra-submit");
        draft.AuthoredByCreatorId.Should().Be(creatorProfile.Id);

        // Re-read from a fresh tracking-clean context so the submit handler sees the
        // persisted RowVersion exactly as the API would.
        var rowVersion = (byte[])draft.RowVersion.Clone();
        dbContext.ChangeTracker.Clear();

        var submitHandler = new SubmitBlogForReviewCommandHandler(
            blogRepository:        Repository(dbContext),
            currentUser:           CurrentUser(TestUserId),
            authorHierarchyGuard:  PermissiveGuard(),
            unitOfWork:            UnitOfWork(dbContext),
            cache:                 Substitute.For<HybridCache>(),
            logger:                NullLogger<SubmitBlogForReviewCommandHandler>.Instance);

        var submitResult = await submitHandler.Handle(
            new SubmitBlogForReviewCommand(draft.Id, rowVersion),
            CancellationToken.None);

        submitResult.IsSuccess.Should().BeTrue(
            "creator-authored drafts must be allowed to enter the review queue");
        var afterSubmit = await dbContext.Blogs.AsNoTracking().FirstAsync(b => b.Id == draft.Id);
        afterSubmit.Status.Should().Be(BlogStatus.PendingReview);
        afterSubmit.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitForReview_Fails_NotCreatorAuthored_ForLegacyAuthorDraft()
    {
        // Regression guard: a draft created without a CreatorProfile must still
        // produce Blog.NotCreatorAuthored on submit. This proves the existing
        // domain rule is preserved by the create-handler change.
        await using var dbContext = CreateDbContext();

        var createHandler = CreateCreateHandler(dbContext); // default: no creator profile

        var createResult = await createHandler.Handle(
            SampleCreateCommand() with { Slug = "non-creator-draft" },
            CancellationToken.None);
        createResult.IsSuccess.Should().BeTrue();

        var draft = await dbContext.Blogs.FirstAsync(b => b.Slug == "non-creator-draft");
        draft.AuthoredByCreatorId.Should().BeNull();

        var rowVersion = (byte[])draft.RowVersion.Clone();
        dbContext.ChangeTracker.Clear();

        var submitHandler = new SubmitBlogForReviewCommandHandler(
            blogRepository:        Repository(dbContext),
            currentUser:           CurrentUser(TestUserId),
            authorHierarchyGuard:  PermissiveGuard(),
            unitOfWork:            UnitOfWork(dbContext),
            cache:                 Substitute.For<HybridCache>(),
            logger:                NullLogger<SubmitBlogForReviewCommandHandler>.Instance);

        var submitResult = await submitHandler.Handle(
            new SubmitBlogForReviewCommand(draft.Id, rowVersion),
            CancellationToken.None);

        submitResult.IsSuccess.Should().BeFalse();
        submitResult.Errors[0].Code.Should().Be("Blog.NotCreatorAuthored");
    }

    [Fact]
    public async Task CreateBlog_DoesNotSetAuthoredByCreatorId_WhenCreatorProfileIsNotActive()
    {
        // GetByUserIdAsync returns null for non-Active profiles (Suspended / Deactivated).
        // We exercise that contract through the real repository: seed a Suspended profile
        // and confirm the blog falls back to the non-creator path.
        await using var dbContext = CreateDbContext();

        var suspended = SeedActiveCreatorProfile(dbContext, TestUserId);
        suspended.Suspend(adminId: Guid.NewGuid(), reason: "test");
        await dbContext.SaveChangesAsync();

        var handler = CreateCreateHandler(
            dbContext,
            creatorProfileRepository: new CreatorProfileRepository(dbContext));

        var result = await handler.Handle(
            SampleCreateCommand() with { Slug = "suspended-creator-draft" },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = await dbContext.Blogs.FirstAsync(b => b.Slug == "suspended-creator-draft");
        saved.AuthoredByCreatorId.Should().BeNull();
        saved.AuthorId.Should().Be(TestUserId);
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
                BlogId:     Guid.NewGuid(),
                RowVersion: SomeRowVersion,
                Title:      "New Title",
                Slug:       "new-slug",
                Content:    ValidContent),
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
                BlogId:     blog.Id,
                RowVersion: blog.RowVersion,
                Title:      "Updated",
                Slug:       "taken",
                Content:    ValidContent),
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
                BlogId:     blog.Id,
                RowVersion: blog.RowVersion,
                Title:      "Updated Title",
                Slug:       "updated-slug",
                Content:    ValidContent),
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
                BlogId:     blog.Id,
                RowVersion: blog.RowVersion,
                Title:      blog.Title,
                Slug:       "renamed",
                Content:    ValidContent),
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
            new DeleteBlogCommand(Guid.NewGuid(), SomeRowVersion),
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
            new DeleteBlogCommand(blog.Id, blog.RowVersion),
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

        await handler.Handle(new DeleteBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

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
            new PublishBlogCommand(Guid.NewGuid(), SomeRowVersion),
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
            new PublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

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
            new PublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

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

        await handler.Handle(new PublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

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
            new UnpublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

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
            new UnpublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

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
            new ArchiveBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

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
            new ArchiveBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await dbContext.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(BlogStatus.Archived);
    }

    // ── RowVersion pre-flight concurrency ─────────────────────────────────────

    [Fact]
    public async Task UpdateBlog_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "rv-update");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        SetRowVersion(blog, [0xAA, 0xBB, 0xCC, 0xDD]);

        var cache = Substitute.For<HybridCache>();
        var handler = CreateUpdateHandler(dbContext, cache: cache);

        var result = await handler.Handle(
            new UpdateBlogCommand(
                BlogId:     blog.Id,
                RowVersion: StaleRowVersion,
                Title:      blog.Title,
                Slug:       blog.Slug,
                Content:    ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");

        // No cache eviction on the mismatch path.
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBlog_Continues_WhenRowVersionMatches()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "rv-update-ok");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        var token = new byte[] { 0x10, 0x20, 0x30, 0x40 };
        SetRowVersion(blog, token);

        var handler = CreateUpdateHandler(dbContext);
        var result = await handler.Handle(
            new UpdateBlogCommand(
                BlogId:     blog.Id,
                RowVersion: token,
                Title:      "RV ok",
                Slug:       "rv-update-renamed",
                Content:    ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBlog_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "rv-delete");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        SetRowVersion(blog, [0xAA]);

        var handler = CreateDeleteHandler(dbContext);
        var result = await handler.Handle(
            new DeleteBlogCommand(blog.Id, StaleRowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");

        // SaveChanges must NOT have run — blog must still exist.
        (await dbContext.Blogs.IgnoreQueryFilters().AnyAsync(b => b.Id == blog.Id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task PublishBlog_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "rv-publish");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        SetRowVersion(blog, [0xAA]);

        var handler = CreatePublishHandler(dbContext);
        var result = await handler.Handle(
            new PublishBlogCommand(blog.Id, StaleRowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");

        var reloaded = await dbContext.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(BlogStatus.Draft,
            "RowVersion mismatch must abort before domain mutation");
    }

    [Fact]
    public async Task UnpublishBlog_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "rv-unpublish");
        blog.Publish(DateTime.UtcNow);
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        SetRowVersion(blog, [0xAA]);

        var handler = CreateUnpublishHandler(dbContext);
        var result = await handler.Handle(
            new UnpublishBlogCommand(blog.Id, StaleRowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task ArchiveBlog_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "rv-archive");
        blog.Publish(DateTime.UtcNow);
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        SetRowVersion(blog, [0xAA]);

        var handler = CreateArchiveHandler(dbContext);
        var result = await handler.Handle(
            new ArchiveBlogCommand(blog.Id, StaleRowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task RowVersionMismatch_DoesNotInvalidateCache()
    {
        await using var dbContext = CreateDbContext();
        var blog = NewBlog(slug: "rv-cache");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();
        SetRowVersion(blog, [0x99]);

        var cache = Substitute.For<HybridCache>();

        // Cover all four state-transition handlers in one assertion.
        await CreateDeleteHandler(dbContext, cache).Handle(
            new DeleteBlogCommand(blog.Id, StaleRowVersion), CancellationToken.None);
        await CreatePublishHandler(dbContext, cache).Handle(
            new PublishBlogCommand(blog.Id, StaleRowVersion), CancellationToken.None);
        await CreateUnpublishHandler(dbContext, cache).Handle(
            new UnpublishBlogCommand(blog.Id, StaleRowVersion), CancellationToken.None);
        await CreateArchiveHandler(dbContext, cache).Handle(
            new ArchiveBlogCommand(blog.Id, StaleRowVersion), CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Author-hierarchy guard — handler-level wiring ─────────────────────────

    [Fact]
    public async Task UpdateBlog_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = CreateDbContext();
        var blog = NewBlog("hierarchy-update");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreateUpdateHandler(db, cache: cache, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new UpdateBlogCommand(
                BlogId:     blog.Id,
                RowVersion: blog.RowVersion,
                Title:      "Updated",
                Slug:       "updated-slug",
                Content:    ValidContent),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");

        // No mutation persisted.
        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Title.Should().Be(blog.Title);

        // No cache eviction on the forbidden path.
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBlog_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = CreateDbContext();
        var blog = NewBlog("hierarchy-delete");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreateDeleteHandler(db, cache: cache, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new DeleteBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");

        // Blog still exists; not soft-deleted.
        (await db.Blogs.IgnoreQueryFilters().FirstAsync(b => b.Id == blog.Id))
            .IsDeleted.Should().BeFalse();

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBlog_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = CreateDbContext();
        var blog = NewBlog("hierarchy-publish");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreatePublishHandler(db, cache: cache, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new PublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");

        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(BlogStatus.Draft, "no publish mutation occurred");

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnpublishBlog_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = CreateDbContext();
        var blog = NewBlog("hierarchy-unpublish");
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreateUnpublishHandler(db, cache: cache, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new UnpublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");

        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(BlogStatus.Published, "no unpublish mutation occurred");
    }

    [Fact]
    public async Task ArchiveBlog_ReturnsForbidden_WhenActorCannotManageAuthor()
    {
        await using var db = CreateDbContext();
        var blog = NewBlog("hierarchy-archive");
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = CreateArchiveHandler(db, cache: cache, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new ArchiveBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");

        var reloaded = await db.Blogs.AsNoTracking().FirstAsync(b => b.Id == blog.Id);
        reloaded.Status.Should().Be(BlogStatus.Published, "no archive mutation occurred");
    }

    // ── No-leak ordering: hierarchy is checked BEFORE RowVersion ──────────────

    [Fact]
    public async Task ForbiddenHierarchy_DoesNotCheckRowVersionFirst()
    {
        // CRITICAL invariant: a stale RowVersion combined with a forbidden
        // hierarchy MUST surface as Forbidden, NOT Conflict.  Otherwise an
        // attacker could detect concurrency state of content they have no
        // right to manage.
        await using var db = CreateDbContext();
        var blog = NewBlog("hierarchy-ordering");
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = CreateUpdateHandler(db, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new UpdateBlogCommand(
                BlogId:     blog.Id,
                RowVersion: StaleRowVersion, // deliberately stale
                Title:      "Updated",
                Slug:       "updated-slug",
                Content:    ValidContent),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden,
            "hierarchy is checked BEFORE RowVersion; stale-RowVersion path must NOT leak");
        result.Errors[0].Code.Should().Be("Blog.AuthorHierarchyForbidden");
        result.Errors[0].Code.Should().NotBe("Blog.ConcurrencyConflict");
    }

    [Fact]
    public async Task ForbiddenHierarchy_DoesNotInvalidateCache_AcrossAllHandlers()
    {
        await using var db = CreateDbContext();
        var blog = NewBlog("hierarchy-no-cache");
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var guard = ForbiddenGuard();

        // Hit every state-transition handler with a forbidden guard;
        // none of them should invalidate cache.
        await CreateUpdateHandler(db, cache, guard).Handle(
            new UpdateBlogCommand(blog.Id, blog.RowVersion, blog.Title, blog.Slug, ValidContent),
            CancellationToken.None);
        await CreateDeleteHandler(db, cache, guard).Handle(
            new DeleteBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);
        await CreatePublishHandler(db, cache, guard).Handle(
            new PublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);
        await CreateUnpublishHandler(db, cache, guard).Handle(
            new UnpublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);
        await CreateArchiveHandler(db, cache, guard).Handle(
            new ArchiveBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForbiddenHierarchy_DoesNotSaveChanges()
    {
        await using var db = CreateDbContext();
        var blog = NewBlog("hierarchy-no-save");
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var guard = ForbiddenGuard();
        var originalStatus = blog.Status;
        var originalTitle = blog.Title;

        await CreateUpdateHandler(db, guard: guard).Handle(
            new UpdateBlogCommand(blog.Id, blog.RowVersion, "Hacked", "hacked", ValidContent),
            CancellationToken.None);
        await CreatePublishHandler(db, guard: guard).Handle(
            new PublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);
        await CreateUnpublishHandler(db, guard: guard).Handle(
            new UnpublishBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);
        await CreateArchiveHandler(db, guard: guard).Handle(
            new ArchiveBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);
        await CreateDeleteHandler(db, guard: guard).Handle(
            new DeleteBlogCommand(blog.Id, blog.RowVersion), CancellationToken.None);

        var reloaded = await db.Blogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(b => b.Id == blog.Id);

        reloaded.Title.Should().Be(originalTitle, "no Update mutation persisted");
        reloaded.Status.Should().Be(originalStatus, "no state-transition mutation persisted");
        reloaded.IsDeleted.Should().BeFalse("no Delete mutation persisted");
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
        IActiveLanguageProvider? languageProvider = null,
        ICreatorProfileRepository? creatorProfileRepository = null) =>
        new(
            blogRepository:           Repository(dbContext),
            creatorProfileRepository: creatorProfileRepository ?? NoCreatorProfileRepository(),
            activeLanguageProvider:   languageProvider ?? LanguageProviderWithEnglish(),
            unitOfWork:               UnitOfWork(dbContext),
            cache:                    cache ?? Substitute.For<HybridCache>(),
            currentUser:              CurrentUser((userId ?? DefaultUser).Value),
            translationOrchestrator:  Substitute.For<IEntityTranslationOrchestrator>(),
            logger:                   NullLogger<CreateBlogCommandHandler>.Instance);

    /// <summary>
    /// Default test double: the author has no CreatorProfile, so blogs are
    /// created via <c>Blog.Create</c> (non-creator path). Most existing tests
    /// rely on this baseline.
    /// </summary>
    private static ICreatorProfileRepository NoCreatorProfileRepository()
    {
        var repo = Substitute.For<ICreatorProfileRepository>();
        repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((CreatorProfile?)null);
        return repo;
    }

    /// <summary>
    /// Inserts an Active CreatorProfile for the given user into the in-memory
    /// store and returns it. Caller is responsible for SaveChanges.
    /// </summary>
    private static CreatorProfile SeedActiveCreatorProfile(
        ContentBlogsDbContext dbContext, Guid userId)
    {
        var createResult = CreatorProfile.Create(
            userId:        userId,
            applicationId: Guid.NewGuid(),
            slug:          $"creator-{userId:N}".Substring(0, 24),
            displayName:   "Test Creator",
            bio:           null,
            avatarUrl:     null);
        createResult.IsSuccess.Should().BeTrue(
            "CreatorProfile.Create must succeed in test fixtures");
        var profile = createResult.Value!;
        dbContext.CreatorProfiles.Add(profile);
        return profile;
    }

    /// <summary>
    /// Returns an <see cref="IBlogAuthorHierarchyGuard"/> that always allows the
    /// caller through.  Used by every existing test except the dedicated
    /// hierarchy regression cases so that pre-existing scenarios continue to
    /// focus on the behaviour they were originally written to verify.
    /// </summary>
    private static IBlogAuthorHierarchyGuard PermissiveGuard()
    {
        var guard = Substitute.For<IBlogAuthorHierarchyGuard>();
        guard.EnsureCanManageBlogOwnedByAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        return guard;
    }

    /// <summary>
    /// Returns a guard that ALWAYS refuses with
    /// <c>Blog.AuthorHierarchyForbidden</c> / <c>Outcome.Forbidden</c>.  Used by
    /// the dedicated regression tests in this file to drive the
    /// hierarchy-failure path through every handler.
    /// </summary>
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

    private static UpdateBlogCommandHandler CreateUpdateHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:        Repository(dbContext),
            authorHierarchyGuard:  guard ?? PermissiveGuard(),
            unitOfWork:            UnitOfWork(dbContext),
            cache:                 cache ?? Substitute.For<HybridCache>(),
            logger:                NullLogger<UpdateBlogCommandHandler>.Instance);

    private static DeleteBlogCommandHandler CreateDeleteHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:        Repository(dbContext),
            authorHierarchyGuard:  guard ?? PermissiveGuard(),
            unitOfWork:            UnitOfWork(dbContext),
            cache:                 cache ?? Substitute.For<HybridCache>(),
            logger:                NullLogger<DeleteBlogCommandHandler>.Instance);

    private static PublishBlogCommandHandler CreatePublishHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null,
        IActiveLanguageProvider? languageProvider = null) =>
        new(
            blogRepository:          Repository(dbContext),
            authorHierarchyGuard:    guard ?? PermissiveGuard(),
            activeLanguageProvider:  languageProvider ?? LanguageProviderWithEnglish(),
            unitOfWork:              UnitOfWork(dbContext),
            cache:                   cache ?? Substitute.For<HybridCache>(),
            logger:                  NullLogger<PublishBlogCommandHandler>.Instance);

    private static UnpublishBlogCommandHandler CreateUnpublishHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:        Repository(dbContext),
            authorHierarchyGuard:  guard ?? PermissiveGuard(),
            unitOfWork:            UnitOfWork(dbContext),
            cache:                 cache ?? Substitute.For<HybridCache>(),
            logger:                NullLogger<UnpublishBlogCommandHandler>.Instance);

    private static ArchiveBlogCommandHandler CreateArchiveHandler(
        ContentBlogsDbContext dbContext,
        HybridCache? cache = null,
        IBlogAuthorHierarchyGuard? guard = null) =>
        new(
            blogRepository:        Repository(dbContext),
            authorHierarchyGuard:  guard ?? PermissiveGuard(),
            unitOfWork:            UnitOfWork(dbContext),
            cache:                 cache ?? Substitute.For<HybridCache>(),
            logger:                NullLogger<ArchiveBlogCommandHandler>.Instance);

    /// <summary>
    /// Sets <see cref="YallaJo.SharedKernel.Domain.Entities.AuditableEntity.RowVersion"/>
    /// on a tracked entity via reflection, because the EF Core InMemory provider
    /// does NOT auto-populate <c>[Timestamp]</c> values the way SQL Server does.
    /// Tests need a deterministic non-empty token to exercise the pre-flight
    /// concurrency check on both the match and mismatch paths.
    /// </summary>
    private static void SetRowVersion(Blog blog, byte[] value)
    {
        var prop = typeof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity)
            .GetProperty(
                nameof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity.RowVersion),
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
        prop!.SetValue(blog, value);
    }
}
