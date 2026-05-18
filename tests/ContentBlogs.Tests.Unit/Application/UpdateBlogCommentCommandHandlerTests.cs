using System.Reflection;
using ContentBlogs.Application.Authorization;
using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.BlogComment.UpdateBlogComment;
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
/// Handler tests for <see cref="UpdateBlogCommentCommandHandler"/>.
///
/// Verifies the project-wide handler ordering invariant:
///   1. Load comment.
///   2. NotFound short-circuit.
///   3. Authorization guard (owner-window OR moderation hierarchy).
///   4. RowVersion pre-flight (returns 409 on mismatch).
///   5. Domain mutation.
///   6. SaveChanges (+ DbUpdateConcurrencyException → 409 mapping).
///   7. Cache invalidation only on success.
/// </summary>
public sealed class UpdateBlogCommentCommandHandlerTests
{
    private static readonly byte[] StaleRowVersion = [0xDE, 0xAD, 0xBE, 0xEF];
    private static readonly byte[] SomeRowVersion = [0x01, 0x02, 0x03, 0x04];

    // ── NotFound ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBlogComment_ReturnsNotFound_WhenCommentMissing()
    {
        await using var db = NewDb();
        var handler = NewHandler(db, guard: PermissiveGuard());

        var result = await handler.Handle(
            new UpdateBlogCommentCommand(
                CommentId:  Guid.NewGuid(),
                RowVersion: SomeRowVersion,
                Content:    "updated content"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("BlogComment.NotFound");
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBlogComment_ReturnsForbidden_WhenAuthorizationFails()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);

        var handler = NewHandler(db, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new UpdateBlogCommentCommand(comment.Id, comment.RowVersion, "edit"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden);
    }

    [Fact]
    public async Task UpdateBlogComment_ReturnsForbidden_BeforeRowVersionConflict_NoLeak()
    {
        // No-leak invariant: when the actor is forbidden AND has a stale
        // RowVersion, the response MUST be Forbidden, never Conflict.
        // Otherwise an attacker could detect content state they have no right
        // to read.
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        SetRowVersion(comment, [0xAA, 0xBB]);

        var handler = NewHandler(db, guard: ForbiddenGuard());

        var result = await handler.Handle(
            new UpdateBlogCommentCommand(comment.Id, StaleRowVersion, "edit"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Forbidden,
            "authorization is checked BEFORE RowVersion; stale RowVersion must not leak");
        result.Errors[0].Code.Should().NotBe("BlogComment.ConcurrencyConflict");
    }

    // ── RowVersion ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBlogComment_ReturnsConflict_WhenRowVersionMismatch()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        SetRowVersion(comment, [0xAA, 0xBB, 0xCC]);

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, guard: PermissiveGuard(), cache: cache);

        var result = await handler.Handle(
            new UpdateBlogCommentCommand(comment.Id, StaleRowVersion, "edit"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("BlogComment.ConcurrencyConflict");

        // Cache must NOT be invalidated on the mismatch path.
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());

        // Content must NOT have been mutated.
        var reloaded = await db.BlogComments.AsNoTracking().FirstAsync(c => c.Id == comment.Id);
        reloaded.Content.Should().Be(comment.Content);
    }

    // ── Happy paths ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBlogComment_UpdatesContent_WhenAuthorizationAndRowVersionPass()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        var token = new byte[] { 0x10, 0x20, 0x30 };
        SetRowVersion(comment, token);

        var handler = NewHandler(db, guard: PermissiveGuard());

        var result = await handler.Handle(
            new UpdateBlogCommentCommand(comment.Id, token, "edited body"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var reloaded = await db.BlogComments.AsNoTracking().FirstAsync(c => c.Id == comment.Id);
        reloaded.Content.Should().Be("edited body");
        reloaded.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateBlogComment_ReturnsConflict_WhenCommentRedacted()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        comment.Redact(DateTime.UtcNow);
        await db.SaveChangesAsync();

        var token = comment.RowVersion;
        var handler = NewHandler(db, guard: PermissiveGuard());

        var result = await handler.Handle(
            new UpdateBlogCommentCommand(comment.Id, token, "attempted edit"),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("BlogComment.Redacted");
    }

    // ── Cache invalidation ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBlogComment_InvalidatesBlogCommentsTag_AfterSuccess()
    {
        await using var db = NewDb();
        var (blog, comment) = await SeedBlogAndComment(db);
        var token = new byte[] { 0x10 };
        SetRowVersion(comment, token);

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, guard: PermissiveGuard(), cache: cache);

        await handler.Handle(
            new UpdateBlogCommentCommand(comment.Id, token, "edited"),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogCommentsTag(blog.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBlogComment_DoesNotInvalidateCache_OnFailurePaths()
    {
        // Validate the no-leak contract across the three failure modes that
        // happen BEFORE SaveChanges: NotFound, Forbidden, ConcurrencyConflict.
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        SetRowVersion(comment, [0x99]);

        var cache = Substitute.For<HybridCache>();

        // NotFound
        await NewHandler(db, guard: PermissiveGuard(), cache: cache).Handle(
            new UpdateBlogCommentCommand(Guid.NewGuid(), SomeRowVersion, "edit"),
            CancellationToken.None);

        // Forbidden
        await NewHandler(db, guard: ForbiddenGuard(), cache: cache).Handle(
            new UpdateBlogCommentCommand(comment.Id, comment.RowVersion, "edit"),
            CancellationToken.None);

        // ConcurrencyConflict
        await NewHandler(db, guard: PermissiveGuard(), cache: cache).Handle(
            new UpdateBlogCommentCommand(comment.Id, StaleRowVersion, "edit"),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-update-comment-{Guid.NewGuid():N}")
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
        g.ResolveEditAuthorizationAsync(
            Arg.Any<BlogCommentEntity>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        g.ResolveDeleteAuthorizationAsync(
            Arg.Any<BlogCommentEntity>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        return g;
    }

    private static IBlogCommentAuthorizationGuard ForbiddenGuard()
    {
        var g = Substitute.For<IBlogCommentAuthorizationGuard>();
        var forbidden = Result.Failure(
            new Error(
                "BlogComment.EditWindowExpired",
                "Comments can only be edited by their owner within 30 minutes of posting."),
            Outcome.Forbidden);
        g.ResolveEditAuthorizationAsync(
            Arg.Any<BlogCommentEntity>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(forbidden);
        g.ResolveDeleteAuthorizationAsync(
            Arg.Any<BlogCommentEntity>(), Arg.Any<CancellationToken>())
            .Returns(forbidden);
        return g;
    }

    private static IContentBlogsUnitOfWork UnitOfWorkOver(ContentBlogsDbContext db)
    {
        var uow = Substitute.For<IContentBlogsUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));
        return uow;
    }

    private static UpdateBlogCommentCommandHandler NewHandler(
        ContentBlogsDbContext db,
        IBlogCommentAuthorizationGuard guard,
        HybridCache? cache = null) =>
        new(
            blogCommentRepository: new BlogCommentRepository(db),
            authorizationGuard:    guard,
            unitOfWork:            UnitOfWorkOver(db),
            cache:                 cache ?? Substitute.For<HybridCache>(),
            logger:                NullLogger<UpdateBlogCommentCommandHandler>.Instance);

    /// <summary>
    /// Sets the private RowVersion via reflection — EF InMemory does not
    /// auto-populate <c>[Timestamp]</c> values like SQL Server.  Matches the
    /// approach already used in <c>BlogLifecycleCommandHandlerTests</c>.
    /// </summary>
    private static void SetRowVersion(BlogCommentEntity comment, byte[] value)
    {
        var prop = typeof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity)
            .GetProperty(
                nameof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity.RowVersion),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop!.SetValue(comment, value);
    }
}
