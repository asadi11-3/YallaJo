using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.BlogComment.AddOrReplaceBlogCommentReaction;
using ContentBlogs.Application.Commands.BlogComment.RemoveBlogCommentReaction;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogEntity = ContentBlogs.Domain.Entities.Blog;
using BlogCommentEntity = ContentBlogs.Domain.Entities.BlogComment;
using BlogStatusEnum = ContentBlogs.Domain.Enums.BlogStatus;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Handler tests for the two BlogCommentReaction command handlers:
///   • <see cref="AddOrReplaceBlogCommentReactionCommandHandler"/>
///   • <see cref="RemoveBlogCommentReactionCommandHandler"/>
///
/// <para>
/// Reactions are an aggregate-root operation on <c>BlogComment</c> — there is
/// no standalone <c>BlogCommentReaction</c> repository.  The handlers load the
/// parent comment with its <c>Reactions</c> collection eagerly, then delegate
/// to <c>AddOrReplaceReaction</c> / <c>RemoveReaction</c> on the aggregate.
/// </para>
/// </summary>
public sealed class BlogCommentReactionCommandHandlerTests
{
    // ── AddOrReplaceBlogCommentReaction ───────────────────────────────────────

    [Fact]
    public async Task AddOrReplaceReaction_ReturnsUnauthorized_WhenUserMissing()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);

        var handler = NewAddHandler(db, userId: AnonymousUser);

        var result = await handler.Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Like),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors[0].Code.Should().Be("BlogCommentReaction.Unauthorized");
    }

    [Fact]
    public async Task AddOrReplaceReaction_ReturnsNotFound_WhenCommentMissing()
    {
        await using var db = NewDb();
        var handler = NewAddHandler(db);

        var result = await handler.Handle(
            new AddOrReplaceBlogCommentReactionCommand(Guid.NewGuid(), ReactionType.Like),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("BlogComment.NotFound");
    }

    [Fact]
    public async Task AddOrReplaceReaction_ReturnsConflict_WhenCommentRedacted()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        comment.Redact(DateTime.UtcNow);
        await db.SaveChangesAsync();

        var result = await NewAddHandler(db).Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Like),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Errors[0].Code.Should().Be("BlogComment.Redacted");
    }

    [Fact]
    public async Task AddOrReplaceReaction_AddsReaction_WhenNoneExists()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        var userId = Guid.NewGuid();

        var result = await NewAddHandler(db, userId: userId).Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Like),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var saved = await db.BlogComments
            .Include(c => c.Reactions)
            .AsNoTracking()
            .FirstAsync(c => c.Id == comment.Id);
        saved.Reactions.Should().ContainSingle();
        saved.Reactions.Single().UserId.Should().Be(userId);
        saved.Reactions.Single().ReactionType.Should().Be(ReactionType.Like);
    }

    [Fact]
    public async Task AddOrReplaceReaction_ReplacesReaction_WhenSameUserWithDifferentType()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        var userId = Guid.NewGuid();

        // First reaction.
        await NewAddHandler(db, userId: userId).Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Like),
            CancellationToken.None);

        // Replace with a different type.
        var result = await NewAddHandler(db, userId: userId).Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Love),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var saved = await db.BlogComments
            .Include(c => c.Reactions)
            .AsNoTracking()
            .FirstAsync(c => c.Id == comment.Id);
        saved.Reactions.Should().ContainSingle("aggregate enforces one reaction per user");
        saved.Reactions.Single().ReactionType.Should().Be(ReactionType.Love);
    }

    [Fact]
    public async Task AddOrReplaceReaction_IsIdempotent_WhenSameReactionExists()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        var userId = Guid.NewGuid();

        await NewAddHandler(db, userId: userId).Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Like),
            CancellationToken.None);

        // Repeat same reaction — must succeed and not duplicate.
        var result = await NewAddHandler(db, userId: userId).Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Like),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var saved = await db.BlogComments
            .Include(c => c.Reactions)
            .AsNoTracking()
            .FirstAsync(c => c.Id == comment.Id);
        saved.Reactions.Should().ContainSingle();
        saved.Reactions.Single().ReactionType.Should().Be(ReactionType.Like);
    }

    [Fact]
    public async Task AddOrReplaceReaction_InvalidatesBlogCommentsTag_AfterSuccess()
    {
        await using var db = NewDb();
        var (blog, comment) = await SeedBlogAndComment(db);

        var cache = Substitute.For<HybridCache>();
        var handler = NewAddHandler(db, cache: cache);

        await handler.Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Like),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogCommentsTag(blog.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrReplaceReaction_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();

        // NotFound
        await NewAddHandler(db, cache: cache).Handle(
            new AddOrReplaceBlogCommentReactionCommand(Guid.NewGuid(), ReactionType.Like),
            CancellationToken.None);

        // Unauthorized
        await NewAddHandler(db, userId: AnonymousUser, cache: cache).Handle(
            new AddOrReplaceBlogCommentReactionCommand(Guid.NewGuid(), ReactionType.Like),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── RemoveBlogCommentReaction ─────────────────────────────────────────────

    [Fact]
    public async Task RemoveReaction_ReturnsUnauthorized_WhenUserMissing()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);

        var result = await NewRemoveHandler(db, userId: AnonymousUser).Handle(
            new RemoveBlogCommentReactionCommand(comment.Id),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Errors[0].Code.Should().Be("BlogCommentReaction.Unauthorized");
    }

    [Fact]
    public async Task RemoveReaction_ReturnsNotFound_WhenCommentMissing()
    {
        await using var db = NewDb();

        var result = await NewRemoveHandler(db).Handle(
            new RemoveBlogCommentReactionCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("BlogComment.NotFound");
    }

    [Fact]
    public async Task RemoveReaction_RemovesExistingReaction()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);
        var userId = Guid.NewGuid();
        await NewAddHandler(db, userId: userId).Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Helpful),
            CancellationToken.None);

        var result = await NewRemoveHandler(db, userId: userId).Handle(
            new RemoveBlogCommentReactionCommand(comment.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var saved = await db.BlogComments
            .Include(c => c.Reactions)
            .AsNoTracking()
            .FirstAsync(c => c.Id == comment.Id);
        saved.Reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveReaction_ReturnsSuccess_WhenReactionMissing_IsIdempotent()
    {
        await using var db = NewDb();
        var (_, comment) = await SeedBlogAndComment(db);

        var result = await NewRemoveHandler(db).Handle(
            new RemoveBlogCommentReactionCommand(comment.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(
            "removing a non-existent reaction is idempotent — handler still surfaces Success");
    }

    [Fact]
    public async Task RemoveReaction_InvalidatesBlogCommentsTag_OnlyWhenReactionRemoved()
    {
        await using var db = NewDb();
        var (blog, comment) = await SeedBlogAndComment(db);
        var userId = Guid.NewGuid();
        await NewAddHandler(db, userId: userId).Handle(
            new AddOrReplaceBlogCommentReactionCommand(comment.Id, ReactionType.Like),
            CancellationToken.None);

        var cache = Substitute.For<HybridCache>();

        // Phase 1: idempotent no-op (different user) → no cache invalidation.
        await NewRemoveHandler(db, userId: Guid.NewGuid(), cache: cache).Handle(
            new RemoveBlogCommentReactionCommand(comment.Id),
            CancellationToken.None);
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());

        // Phase 2: actual removal → cache MUST be invalidated.
        await NewRemoveHandler(db, userId: userId, cache: cache).Handle(
            new RemoveBlogCommentReactionCommand(comment.Id),
            CancellationToken.None);
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogCommentsTag(blog.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveReaction_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var cache = Substitute.For<HybridCache>();

        // NotFound
        await NewRemoveHandler(db, cache: cache).Handle(
            new RemoveBlogCommentReactionCommand(Guid.NewGuid()),
            CancellationToken.None);

        // Unauthorized
        await NewRemoveHandler(db, userId: AnonymousUser, cache: cache).Handle(
            new RemoveBlogCommentReactionCommand(Guid.NewGuid()),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-reaction-{Guid.NewGuid():N}")
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

    private static ICurrentUser CurrentUser(Guid? userId)
    {
        var stub = Substitute.For<ICurrentUser>();
        stub.IsAuthenticated.Returns(userId.HasValue);
        stub.UserId.Returns(userId);
        return stub;
    }

    private static IContentBlogsUnitOfWork UnitOfWorkOver(ContentBlogsDbContext db)
    {
        var uow = Substitute.For<IContentBlogsUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci => db.SaveChangesAsync(ci.Arg<CancellationToken>()));
        return uow;
    }

    /// <summary>
    /// Sentinel: <c>null</c> userId means unauthenticated.  A fresh Guid is
    /// generated for the default authenticated case.
    /// </summary>
    private sealed class UserOption
    {
        public Guid? UserId { get; }

        public UserOption(Guid? userId) { UserId = userId; }
    }

    // Sentinel used to flag "no userId — caller is unauthenticated".  We can't
    // use `Guid?` defaulting to null because every call site that omits userId
    // would then go through the unauthenticated path.  Tests that DO want
    // unauthenticated pass `userId: AnonymousUser` explicitly.
    private static readonly Guid AnonymousUser = Guid.Empty;

    private static AddOrReplaceBlogCommentReactionCommandHandler NewAddHandler(
        ContentBlogsDbContext db,
        Guid? userId = null,
        HybridCache? cache = null)
    {
        Guid? resolved;
        if (userId is null)
        {
            resolved = Guid.NewGuid(); // default authenticated user
        }
        else if (userId.Value == AnonymousUser)
        {
            resolved = null; // explicit unauthenticated
        }
        else
        {
            resolved = userId.Value;
        }

        return BuildAddHandler(db, new UserOption(resolved), cache);
    }

    private static AddOrReplaceBlogCommentReactionCommandHandler BuildAddHandler(
        ContentBlogsDbContext db,
        UserOption user,
        HybridCache? cache) =>
        new(
            blogCommentRepository: new BlogCommentRepository(db),
            unitOfWork:            UnitOfWorkOver(db),
            cache:                 cache ?? Substitute.For<HybridCache>(),
            currentUser:           CurrentUser(user.UserId),
            logger:                NullLogger<AddOrReplaceBlogCommentReactionCommandHandler>.Instance);

    private static RemoveBlogCommentReactionCommandHandler NewRemoveHandler(
        ContentBlogsDbContext db,
        Guid? userId = null,
        HybridCache? cache = null)
    {
        Guid? resolved;
        if (userId is null)
        {
            resolved = Guid.NewGuid();
        }
        else if (userId.Value == AnonymousUser)
        {
            resolved = null;
        }
        else
        {
            resolved = userId.Value;
        }

        return BuildRemoveHandler(db, new UserOption(resolved), cache);
    }

    private static RemoveBlogCommentReactionCommandHandler BuildRemoveHandler(
        ContentBlogsDbContext db,
        UserOption user,
        HybridCache? cache) =>
        new(
            blogCommentRepository: new BlogCommentRepository(db),
            unitOfWork:            UnitOfWorkOver(db),
            cache:                 cache ?? Substitute.For<HybridCache>(),
            currentUser:           CurrentUser(user.UserId),
            logger:                NullLogger<RemoveBlogCommentReactionCommandHandler>.Instance);
}
