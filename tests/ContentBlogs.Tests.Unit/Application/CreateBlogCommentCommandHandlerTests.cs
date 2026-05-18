using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.BlogComment.CreateBlogComment;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Events;
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
/// Handler-level tests for <see cref="CreateBlogCommentCommandHandler"/>.
///
/// Uses EF Core InMemory + real <see cref="BlogRepository"/> /
/// <see cref="BlogCommentRepository"/> so query filters, depth-chain loading,
/// and SaveChanges semantics are exercised end-to-end.  The unit-of-work and
/// cache are NSubstituted so the test can assert side-effects.
/// </summary>
public sealed class CreateBlogCommentCommandHandlerTests
{
    private const string ValidContent = "A thoughtful, non-empty comment body.";

    // ── Authorization gate ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBlogComment_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        await db.SaveChangesAsync();

        var handler = NewHandlerAnonymous(db);

        var result = await handler.Handle(
            new CreateBlogCommentCommand(blog.Id, ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.Unauthorized);
        result.Error!.Code.Should().Be("BlogComment.Unauthorized");
    }

    // ── Blog visibility ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBlogComment_ReturnsNotFound_WhenBlogMissing()
    {
        await using var db = NewDb();
        var handler = NewHandler(db);

        var result = await handler.Handle(
            new CreateBlogCommentCommand(Guid.NewGuid(), ValidContent),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Error!.Code.Should().Be("Blog.NotFound");
    }

    [Fact]
    public async Task CreateBlogComment_ReturnsConflict_WhenBlogDraft()
    {
        await using var db = NewDb();
        var draft = NewBlog(); // Draft by default
        db.Blogs.Add(draft);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(draft.Id, ValidContent),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Error!.Code.Should().Be("BlogComment.BlogNotAcceptingComments");
    }

    [Fact]
    public async Task CreateBlogComment_ReturnsConflict_WhenBlogArchived()
    {
        await using var db = NewDb();
        var archived = NewBlog();
        archived.Publish(DateTime.UtcNow);
        archived.Archive(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(archived);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(archived.Id, ValidContent),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Conflict);
        result.Error!.Code.Should().Be("BlogComment.BlogNotAcceptingComments");
    }

    [Fact]
    public async Task CreateBlogComment_ReturnsNotFound_WhenBlogSoftDeleted()
    {
        // Soft-deleted blogs are hidden by the global query filter, so the
        // handler's GetByIdAsync returns null → NotFound (does not leak state).
        await using var db = NewDb();
        var blog = NewBlog();
        blog.Publish(DateTime.UtcNow);
        blog.Delete(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(blog.Id, ValidContent),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Error!.Code.Should().Be("Blog.NotFound");
    }

    // ── Happy paths ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBlogComment_CreatesRootComment_WhenBlogPublished()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var handler = NewHandler(db, userId: userId);

        var result = await handler.Handle(
            new CreateBlogCommentCommand(blog.Id, ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        result.Value!.CommentId.Should().NotBe(Guid.Empty);

        var saved = await db.BlogComments.AsNoTracking().FirstAsync();
        saved.BlogId.Should().Be(blog.Id);
        saved.UserId.Should().Be(userId);
        saved.Content.Should().Be(ValidContent);
        saved.ParentCommentId.Should().BeNull();
        saved.IsContentRedacted.Should().BeFalse();
    }

    [Fact]
    public async Task CreateBlogComment_CreatesReply_WhenParentBelongsToSameBlog()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var root = BlogCommentEntity.Create(
            blogId:     blog.Id,
            userId:     Guid.NewGuid(),
            content:    "root",
            parent:     null,
            blogStatus: BlogStatusEnum.Published,
            utcNow:     DateTime.UtcNow);
        db.BlogComments.Add(root);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(blog.Id, "a reply", ParentCommentId: root.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reply = await db.BlogComments
            .AsNoTracking()
            .FirstAsync(c => c.ParentCommentId != null);
        reply.ParentCommentId.Should().Be(root.Id);
        reply.BlogId.Should().Be(blog.Id);
    }

    [Fact]
    public async Task CreateBlogComment_ReturnsNotFound_WhenParentMissing()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(blog.Id, ValidContent, ParentCommentId: Guid.NewGuid()),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
        result.Error!.Code.Should().Be("BlogComment.ParentNotFound");
    }

    [Fact]
    public async Task CreateBlogComment_ReturnsInvalid_WhenParentBelongsToDifferentBlog()
    {
        await using var db = NewDb();
        var blogA = AddPublishedBlog(db);
        var blogB = AddPublishedBlog(db);
        var rootOnA = BlogCommentEntity.Create(
            blogId:     blogA.Id,
            userId:     Guid.NewGuid(),
            content:    "root on A",
            parent:     null,
            blogStatus: BlogStatusEnum.Published,
            utcNow:     DateTime.UtcNow);
        db.BlogComments.Add(rootOnA);
        await db.SaveChangesAsync();

        // Try to reply on Blog B with a parent that belongs to Blog A.
        var result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(blogB.Id, ValidContent, ParentCommentId: rootOnA.Id),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.Invalid);
        result.Error!.Code.Should().Be("BlogComment.ParentBlogMismatch");
    }

    [Fact]
    public async Task CreateBlogComment_ReturnsConflict_WhenDepthTooDeep()
    {
        // Build a chain at the maximum allowed depth: root → depth1 → depth2.
        // A reply against depth2 must be rejected as MaxDepthExceeded.
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var root = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "root", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        db.BlogComments.Add(root);
        await db.SaveChangesAsync();

        // Depth-1 reply, persisted via handler so the repository chain-loading
        // path is exercised.
        var depth1Result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(blog.Id, "d1", ParentCommentId: root.Id),
            CancellationToken.None);
        depth1Result.IsSuccess.Should().BeTrue();

        var depth1 = await db.BlogComments.AsNoTracking()
            .FirstAsync(c => c.ParentCommentId == root.Id);

        // Depth-2 reply, persisted.
        var depth2Result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(blog.Id, "d2", ParentCommentId: depth1.Id),
            CancellationToken.None);
        depth2Result.IsSuccess.Should().BeTrue();

        var depth2 = await db.BlogComments.AsNoTracking()
            .FirstAsync(c => c.ParentCommentId == depth1.Id);

        // Depth-3 attempt — must be rejected.
        var depth3Result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(blog.Id, "too deep", ParentCommentId: depth2.Id),
            CancellationToken.None);

        depth3Result.Outcome.Should().Be(Outcome.Conflict);
        depth3Result.Error!.Code.Should().Be("BlogComment.MaxDepthExceeded");
    }

    // ── Cache invalidation ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBlogComment_InvalidatesBlogCommentsTag_AfterSuccess()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache: cache);

        await handler.Handle(
            new CreateBlogCommentCommand(blog.Id, ValidContent),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogCommentsTag(blog.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBlogComment_DoesNotInvalidateCache_OnFailure()
    {
        await using var db = NewDb();
        var draft = NewBlog(); // remains Draft
        db.Blogs.Add(draft);
        await db.SaveChangesAsync();

        var cache = Substitute.For<HybridCache>();
        var handler = NewHandler(db, cache: cache);

        var result = await handler.Handle(
            new CreateBlogCommentCommand(draft.Id, ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    // ── Domain event (internal-only) ──────────────────────────────────────────

    [Fact]
    public async Task CreateBlogComment_RaisesCreatedDomainEvent_OnTrackedEntity()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new CreateBlogCommentCommand(blog.Id, ValidContent),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Reload the tracked aggregate; the domain event should still be on it
        // because we did not run a real MediatR dispatch pipeline.
        var saved = db.ChangeTracker.Entries<BlogCommentEntity>().First().Entity;
        var evt = saved.DomainEvents.OfType<BlogCommentCreatedDomainEvent>().FirstOrDefault();

        evt.Should().NotBeNull("the aggregate must have raised BlogCommentCreatedDomainEvent");
        evt!.BlogId.Should().Be(blog.Id);
        evt.CommentId.Should().Be(saved.Id);
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-create-comment-{Guid.NewGuid():N}")
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static BlogEntity NewBlog() =>
        BlogEntity.Create(
            title:            "Petra Guide",
            slug:             $"blog-{Guid.NewGuid():N}",
            content:          new string('x', 200),
            authorId:         Guid.NewGuid(),
            sourceLanguageId: Guid.NewGuid(),
            utcNow:           DateTime.UtcNow);

    private static BlogEntity AddPublishedBlog(ContentBlogsDbContext db)
    {
        var blog = NewBlog();
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        return blog;
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
    /// Test factory for handlers.  Pass an explicit <c>userId</c> Guid for an
    /// authenticated caller, or omit it for an authenticated caller with a
    /// fresh Guid.  To assert the unauthenticated path, call <see cref="NewHandlerAnonymous"/>.
    /// </summary>
    private static CreateBlogCommentCommandHandler NewHandler(
        ContentBlogsDbContext db,
        Guid? userId = null,
        HybridCache? cache = null) =>
        BuildHandler(db, userId ?? Guid.NewGuid(), cache);

    /// <summary>Factory that wires up an anonymous (unauthenticated) caller.</summary>
    private static CreateBlogCommentCommandHandler NewHandlerAnonymous(
        ContentBlogsDbContext db,
        HybridCache? cache = null) =>
        BuildHandler(db, anonymousUserId: null, cache);

    private static CreateBlogCommentCommandHandler BuildHandler(
        ContentBlogsDbContext db,
        Guid? anonymousUserId,
        HybridCache? cache) =>
        new(
            blogRepository:         new BlogRepository(db),
            blogCommentRepository:  new BlogCommentRepository(db),
            unitOfWork:             UnitOfWorkOver(db),
            cache:                  cache ?? Substitute.For<HybridCache>(),
            currentUser:            CurrentUser(anonymousUserId),
            logger:                 NullLogger<CreateBlogCommentCommandHandler>.Instance);
}
