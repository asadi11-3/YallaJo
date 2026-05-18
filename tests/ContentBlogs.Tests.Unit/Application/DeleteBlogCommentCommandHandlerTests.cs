using System.Reflection;
using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.BlogComment.DeleteBlogComment;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogEntity = ContentBlogs.Domain.Entities.Blog;
using BlogCommentEntity = ContentBlogs.Domain.Entities.BlogComment;
using BlogStatusEnum = ContentBlogs.Domain.Enums.BlogStatus;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Handler tests for <see cref="DeleteBlogCommentCommandHandler"/>.
/// Delete is soft (redaction): row stays, content is replaced with the marker
/// so the thread structure is preserved.  Same handler-order invariant as
/// Update: authorization → RowVersion → mutation → SaveChanges → cache.
/// </summary>
public sealed class DeleteBlogCommentCommandHandlerTests
{
    private static readonly byte[] StaleRowVersion = [0xDE, 0xAD, 0xBE, 0xEF];
    private static readonly byte[] SomeRowVersion = [0x01, 0x02, 0x03, 0x04];

    // ── NotFound ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBlogComment_ReturnsNotFound_WhenCommentMissing()
    {
        await using var db = NewDb();
        var handler = NewHandler(db, guard: PermissiveGuard());

        var result = await handler.Handle(
            new DeleteBlogCommentCommand(Guid.NewGuid(), SomeRowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("BlogComment.NotFound");
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBlogComment_ReturnsForbidden_WhenAuthorizationFails()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);

        var handler = NewHandler(db, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new DeleteBlogCommentCommand(comment.Id, comment.RowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task DeleteBlogComment_ReturnsForbidden_BeforeRowVersionConflict_NoLeak()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        SetRowVersion(comment, [0xAA, 0xBB]);

        var handler = NewHandler(db, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new DeleteBlogCommentCommand(comment.Id, StaleRowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden,
            "authorization is checked BEFORE RowVersion; stale RowVersion must not leak");
        result.Errors[0].Code.Should().NotBe("BlogComment.ConcurrencyConflict");

        // Comment must NOT have been redacted.
        var reloaded = await db.BlogComments.AsNoTracking().FirstAsync(c => c.Id == comment.Id);
        reloaded.IsContentRedacted.Should().BeFalse();
    }

    // ── RowVersion ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBlogComment_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        SetRowVersion(comment, [0xAA]);

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, guard: PermissiveGuard(), cache: cache);

        var result = await handler.Handle(
            new DeleteBlogCommentCommand(comment.Id, StaleRowVersion),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("BlogComment.ConcurrencyConflict");

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());

        var reloaded = await db.BlogComments.AsNoTracking().FirstAsync(c => c.Id == comment.Id);
        reloaded.IsContentRedacted.Should().BeFalse(
            "no mutation must occur on the RowVersion-mismatch path");
    }

    // ── Happy paths ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBlogComment_RedactsComment_WhenAuthorizationAndRowVersionPass()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        var token = new byte[] { 0x10 };
        SetRowVersion(comment, token);

        var handler = NewHandler(db, guard: PermissiveGuard());

        var result = await handler.Handle(
            new DeleteBlogCommentCommand(comment.Id, token),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var reloaded = await db.BlogComments.AsNoTracking().FirstAsync(c => c.Id == comment.Id);
        reloaded.IsContentRedacted.Should().BeTrue();
        reloaded.Content.Should().Be(BlogCommentEntity.RedactedContentMarker);
    }

    [Fact]
    public async Task DeleteBlogComment_IsIdempotent_WhenAlreadyRedacted()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        comment.Redact(DateTime.UtcNow);
        await db.SaveChangesAsync();

        var token = comment.RowVersion;
        var handler = NewHandler(db, guard: PermissiveGuard());

        var result = await handler.Handle(
            new DeleteBlogCommentCommand(comment.Id, token),
            CancellationToken.None);

        // Domain.Redact short-circuits when already redacted — handler still
        // surfaces Success (no Conflict, no exception).
        result.IsSuccess.Should().BeTrue("delete is idempotent on already-redacted comments");

        var reloaded = await db.BlogComments.AsNoTracking().FirstAsync(c => c.Id == comment.Id);
        reloaded.IsContentRedacted.Should().BeTrue();
    }

    // ── Cache invalidation ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteBlogComment_InvalidatesBlogCommentsTag_AfterSuccess()
    {
        await using var db = NewDb();
        var (blog, comment) = await SeedBlogAndComment(db);
        var token = new byte[] { 0x10 };
        SetRowVersion(comment, token);

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, guard: PermissiveGuard(), cache: cache);

        await handler.Handle(
            new DeleteBlogCommentCommand(comment.Id, token),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogCommentsTag(blog.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBlogComment_DoesNotInvalidateCache_OnFailurePaths()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        SetRowVersion(comment, [0xAA]);

        var cache = Substitute.For<HybridCache>();

        // NotFound
        await NewHandler(db, guard: PermissiveGuard(), cache: cache).Handle(
            new DeleteBlogCommentCommand(Guid.NewGuid(), SomeRowVersion),
            CancellationToken.None);

        // Forbidden
        await NewHandler(db, guard: ForbiddenGuard(), cache: cache).Handle(
            new DeleteBlogCommentCommand(comment.Id, comment.RowVersion),
            CancellationToken.None);

        // ConcurrencyConflict
        await NewHandler(db, guard: PermissiveGuard(), cache: cache).Handle(
            new DeleteBlogCommentCommand(comment.Id, StaleRowVersion),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-delete-comment-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static async Task<(BlogEntity Blog, BlogCommentEntity Comment)> SeedBlogAndComment(
        ContentBlogsDbContext db)
    {
        var blog = BlogEntity.Create(
            title:            "Petra Guide",
            slug:             $"blog-{Guid.NewGuid():N}",
            content:          new string('x', 200),
            authorId:         Guid.NewGuid(),
            sourceLanguageId: Guid.NewGuid(),
            utcNow:           DateTime.UtcNow);
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);

        var comment = BlogCommentEntity.Create(
            blogId:     blog.Id,
            userId:     Guid.NewGuid(),
            content:    "a thoughtful comment",
            parent:     null,
            blogStatus: BlogStatusEnum.Published,
            utcNow:     DateTime.UtcNow);
        db.BlogComments.Add(comment);
        await db.SaveChangesAsync();

        return (blog, comment);
    }

    private static IBlogCommentAuthorizationGuard PermissiveGuard()
    {
        var g = Substitute.For<IBlogCommentAuthorizationGuard>();
        g.ResolveDeleteAuthorizationAsync(
            Arg.Any<BlogCommentEntity>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        return g;
    }

    private static IBlogCommentAuthorizationGuard ForbiddenGuard()
    {
        var g = Substitute.For<IBlogCommentAuthorizationGuard>();
        g.ResolveDeleteAuthorizationAsync(
            Arg.Any<BlogCommentEntity>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(
                new Error(
                    "Blog.AuthorHierarchyForbidden",
                    "You cannot manage content created by a user at the same or higher privilege level."),
                Outcome.Forbidden));
        return g;
    }

    private static IContentBlogsUnitOfWork UnitOfWorkOver(ContentBlogsDbContext db)
    {
        var uow = Substitute.For<IContentBlogsUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));
        return uow;
    }

    private static DeleteBlogCommentCommandHandler NewHandler(
        ContentBlogsDbContext db,
        IBlogCommentAuthorizationGuard guard,
        HybridCache? cache = null) =>
        new(
            blogCommentRepository: new BlogCommentRepository(db),
            authorizationGuard:    guard,
            unitOfWork:            UnitOfWorkOver(db),
            cache:                 cache ?? Substitute.For<HybridCache>(),
            logger:                NullLogger<DeleteBlogCommentCommandHandler>.Instance);

    private static void SetRowVersion(BlogCommentEntity comment, byte[] value)
    {
        var prop = typeof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity)
            .GetProperty(
                nameof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity.RowVersion),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop!.SetValue(comment, value);
    }
}
