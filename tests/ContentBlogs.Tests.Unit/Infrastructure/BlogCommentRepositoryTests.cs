using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using BlogEntity = ContentBlogs.Domain.Entities.Blog;
using BlogCommentEntity = ContentBlogs.Domain.Entities.BlogComment;
using BlogStatusEnum = ContentBlogs.Domain.Enums.BlogStatus;

namespace ContentBlogs.Tests.Unit.Infrastructure;

/// <summary>
/// Repository tests for <see cref="BlogCommentRepository"/>.
///
/// Exercises the two specialized read methods used by the command handlers:
///   • <c>GetWithParentChainAsync</c>: eagerly loads <c>ParentComment</c> and
///     <c>ParentComment.ParentComment</c> so the domain depth-check works.
///   • <c>GetWithReactionsAsync</c>: eagerly loads the <c>Reactions</c>
///     collection so the aggregate can enforce one-per-user.
/// Also verifies the query filters hide comments whose parent blog is deleted.
/// </summary>
public sealed class BlogCommentRepositoryTests
{
    [Fact]
    public async Task GetWithParentChainAsync_LoadsTwoLevelChain()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);

        var root = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "root", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        db.BlogComments.Add(root);
        await db.SaveChangesAsync();

        // Re-read tracked with chain to satisfy the domain depth check.
        var trackedRoot = await db.BlogComments
            .Include(c => c.ParentComment)
            .FirstAsync(c => c.Id == root.Id);
        var depth1 = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "d1",
            parent: trackedRoot, BlogStatusEnum.Published, DateTime.UtcNow.AddSeconds(1));
        db.BlogComments.Add(depth1);
        await db.SaveChangesAsync();

        var trackedDepth1 = await db.BlogComments
            .Include(c => c.ParentComment!)
                .ThenInclude(p => p.ParentComment)
            .FirstAsync(c => c.Id == depth1.Id);
        var depth2 = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "d2",
            parent: trackedDepth1, BlogStatusEnum.Published, DateTime.UtcNow.AddSeconds(2));
        db.BlogComments.Add(depth2);
        await db.SaveChangesAsync();

        // Reset the change tracker so the repository must (re-)hydrate the
        // navigation chain from storage, rather than serving cached entities.
        db.ChangeTracker.Clear();

        var repo = new BlogCommentRepository(db);
        var loaded = await repo.GetWithParentChainAsync(depth2.Id);

        loaded.Should().NotBeNull();
        loaded!.ParentComment.Should().NotBeNull("ParentComment must be eagerly loaded");
        loaded.ParentComment!.Id.Should().Be(depth1.Id);
        loaded.ParentComment.ParentComment.Should().NotBeNull(
            "Grandparent (ParentComment.ParentComment) must also be eagerly loaded for the depth check");
        loaded.ParentComment.ParentComment!.Id.Should().Be(root.Id);
    }

    [Fact]
    public async Task GetWithParentChainAsync_ReturnsNullForUnknownId()
    {
        await using var db = NewDb();
        var repo = new BlogCommentRepository(db);

        var result = await repo.GetWithParentChainAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithReactionsAsync_LoadsReactionsCollection()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var comment = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "with-reactions", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        comment.AddOrReplaceReaction(Guid.NewGuid(), global::ContentBlogs.Domain.Enums.ReactionType.Like, DateTime.UtcNow);
        comment.AddOrReplaceReaction(Guid.NewGuid(), global::ContentBlogs.Domain.Enums.ReactionType.Insightful, DateTime.UtcNow);
        db.BlogComments.Add(comment);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var repo = new BlogCommentRepository(db);
        var loaded = await repo.GetWithReactionsAsync(comment.Id);

        loaded.Should().NotBeNull();
        loaded!.Reactions.Should().HaveCount(2,
            "GetWithReactionsAsync must eagerly populate the Reactions collection");
    }

    [Fact]
    public async Task GetWithReactionsAsync_ReturnsNullForUnknownId()
    {
        await using var db = NewDb();
        var repo = new BlogCommentRepository(db);

        var result = await repo.GetWithReactionsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task QueryFilter_HidesComments_WhenParentBlogIsDeleted()
    {
        // Repository (and DbSet<BlogComment>) honor the parent-blog soft-delete
        // filter — comments of a soft-deleted blog must not be returned.
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var comment = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "to be hidden", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        db.BlogComments.Add(comment);
        await db.SaveChangesAsync();

        blog.Delete(DateTime.UtcNow.AddMinutes(1));
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var visible = await db.BlogComments.FirstOrDefaultAsync(c => c.Id == comment.Id);
        visible.Should().BeNull("query filter must exclude comments whose parent blog is soft-deleted");

        var allWithoutFilter = await db.BlogComments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == comment.Id);
        allWithoutFilter.Should().NotBeNull("the row itself is still present in storage");
    }

    [Fact]
    public async Task QueryFilter_HidesComments_WhenCommentIsRowDeleted()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var comment = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "soft-deleted", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        db.BlogComments.Add(comment);
        await db.SaveChangesAsync();

        // Set IsDeleted via reflection to simulate the soft-delete column.
        // Redact() ≠ Delete(): redacted comments STAY visible.
        var prop = typeof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity)
            .GetProperty(
                nameof(YallaJo.SharedKernel.Domain.Entities.AuditableEntity.IsDeleted),
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);
        prop!.SetValue(comment, true);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var visible = await db.BlogComments.FirstOrDefaultAsync(c => c.Id == comment.Id);
        visible.Should().BeNull("query filter must exclude IsDeleted=true comments");
    }

    [Fact]
    public async Task QueryFilter_KeepsRedactedComments_Visible()
    {
        // Approved contract: redaction sets IsContentRedacted=true and replaces
        // Content with the marker, but does NOT set IsDeleted; the row must
        // remain visible so thread structure is preserved.
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var comment = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "before redaction", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        comment.Redact(DateTime.UtcNow.AddSeconds(1));
        db.BlogComments.Add(comment);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var visible = await db.BlogComments.FirstOrDefaultAsync(c => c.Id == comment.Id);
        visible.Should().NotBeNull("redacted comments must remain visible to preserve thread structure");
        visible!.IsContentRedacted.Should().BeTrue();
        visible.Content.Should().Be(BlogCommentEntity.RedactedContentMarker);
    }

    [Fact]
    public async Task ReactionUniqueIndex_RejectsDuplicate_SameUserAndComment()
    {
        // The configuration declares HasIndex(c => new { c.CommentId, c.UserId }).IsUnique()
        // on BlogCommentReaction.  Even though InMemory does not enforce unique
        // indexes at the storage layer, the aggregate-level invariant prevents
        // duplicates from ever reaching SaveChanges.
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var comment = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "for reactions", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        var sameUser = Guid.NewGuid();
        comment.AddOrReplaceReaction(sameUser, global::ContentBlogs.Domain.Enums.ReactionType.Like, DateTime.UtcNow);
        comment.AddOrReplaceReaction(sameUser, global::ContentBlogs.Domain.Enums.ReactionType.Insightful, DateTime.UtcNow);
        db.BlogComments.Add(comment);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await new BlogCommentRepository(db).GetWithReactionsAsync(comment.Id);
        loaded!.Reactions.Should().ContainSingle(
            "the aggregate guarantees one reaction per user — the unique index is the storage-level belt to that suspenders");
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb(string? useSharedName = null)
    {
        var name = useSharedName ?? $"content-blogs-repo-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ContentBlogsDbContext(options);
    }

    private static BlogEntity AddPublishedBlog(ContentBlogsDbContext db)
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
        return blog;
    }
}
