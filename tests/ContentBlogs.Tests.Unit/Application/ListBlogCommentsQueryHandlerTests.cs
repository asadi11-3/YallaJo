using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.BlogComment.ListBlogComments;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using BlogEntity = ContentBlogs.Domain.Entities.Blog;
using BlogCommentEntity = ContentBlogs.Domain.Entities.BlogComment;
using BlogStatusEnum = ContentBlogs.Domain.Enums.BlogStatus;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Query-handler tests for <see cref="ListBlogCommentsQueryHandler"/>.
///
/// <para>
/// The query is anonymous-friendly but must NOT leak blog existence for
/// Draft/Archived/Deleted blogs — all four hidden states map to NotFound.
/// </para>
/// </summary>
public sealed class ListBlogCommentsQueryHandlerTests
{
    // ── Visibility — non-published / deleted blogs return NotFound ────────────

    [Fact]
    public async Task ListBlogComments_ReturnsNotFound_WhenBlogMissing()
    {
        await using var db = NewDb();
        var handler = NewHandler(db);

        var result = await handler.Handle(
            new ListBlogCommentsQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task ListBlogComments_ReturnsNotFound_WhenBlogDraft()
    {
        await using var db = NewDb();
        var draft = NewBlog(); // remains Draft
        db.Blogs.Add(draft);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(draft.Id),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound,
            "Draft blogs must not leak existence to anonymous callers");
    }

    [Fact]
    public async Task ListBlogComments_ReturnsNotFound_WhenBlogArchived()
    {
        await using var db = NewDb();
        var archived = NewBlog();
        archived.Publish(DateTime.UtcNow);
        archived.Archive(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(archived);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(archived.Id),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound);
    }

    [Fact]
    public async Task ListBlogComments_ReturnsNotFound_WhenBlogSoftDeleted()
    {
        await using var db = NewDb();
        var blog = NewBlog();
        blog.Publish(DateTime.UtcNow);
        blog.Delete(DateTime.UtcNow.AddMinutes(1));
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(blog.Id),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound,
            "Soft-deleted blogs must not leak existence to anonymous callers");
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBlogComments_ReturnsComments_WhenBlogPublished()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        for (var i = 0; i < 3; i++)
        {
            db.BlogComments.Add(BlogCommentEntity.Create(
                blogId:     blog.Id,
                userId:     Guid.NewGuid(),
                content:    $"comment {i}",
                parent:     null,
                blogStatus: BlogStatusEnum.Published,
                utcNow:     DateTime.UtcNow.AddSeconds(i)));
        }

        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(blog.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task ListBlogComments_IncludesRedactedComments_WithRedactionMarker()
    {
        // Approved behavior: redacted comments STAY visible to preserve thread
        // structure; their content is replaced with the redaction marker and
        // IsContentRedacted = true so the UI can render "[deleted]" placeholders.
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var visible = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "visible content", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        var redacted = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "soon redacted", null,
            BlogStatusEnum.Published, DateTime.UtcNow.AddSeconds(1));
        redacted.Redact(DateTime.UtcNow.AddSeconds(2));
        db.BlogComments.AddRange(visible, redacted);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(blog.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        var redactedDto = result.Value.Items.Single(c => c.Id == redacted.Id);
        redactedDto.IsContentRedacted.Should().BeTrue();
        redactedDto.Content.Should().Be(BlogCommentEntity.RedactedContentMarker);
    }

    [Fact]
    public async Task ListBlogComments_ReturnsParentAndRepliesTogether()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);

        var root = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "root", null,
            BlogStatusEnum.Published, DateTime.UtcNow);
        db.BlogComments.Add(root);
        await db.SaveChangesAsync();

        // Reload tracked so the ParentComment navigation is populated when we
        // create the reply (the domain validates depth via the loaded chain).
        var trackedRoot = await db.BlogComments
            .Include(c => c.ParentComment)
            .FirstAsync(c => c.Id == root.Id);

        var reply = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "reply",
            parent: trackedRoot,
            BlogStatusEnum.Published,
            DateTime.UtcNow.AddSeconds(1));
        db.BlogComments.Add(reply);
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(blog.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Single(c => c.ParentCommentId is null).Id.Should().Be(root.Id);
        result.Value.Items.Single(c => c.ParentCommentId == root.Id).Id.Should().Be(reply.Id);
    }

    [Fact]
    public async Task ListBlogComments_OrdersByCreatedAtAscending()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var t0 = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var older = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "older", null,
            BlogStatusEnum.Published, t0);
        var newer = BlogCommentEntity.Create(
            blog.Id, Guid.NewGuid(), "newer", null,
            BlogStatusEnum.Published, t0.AddHours(1));
        db.BlogComments.AddRange(newer, older); // insert in reverse to prove ordering
        await db.SaveChangesAsync();

        var result = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(blog.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Select(c => c.Id).Should().Equal(new[] { older.Id, newer.Id });
    }

    [Fact]
    public async Task ListBlogComments_PaginatesCorrectly()
    {
        await using var db = NewDb();
        var blog = AddPublishedBlog(db);
        var t0 = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 5; i++)
        {
            db.BlogComments.Add(BlogCommentEntity.Create(
                blog.Id, Guid.NewGuid(), $"comment-{i}", null,
                BlogStatusEnum.Published, t0.AddMinutes(i)));
        }

        await db.SaveChangesAsync();

        var page1 = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(blog.Id, Page: 1, PageSize: 2),
            CancellationToken.None);
        var page2 = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(blog.Id, Page: 2, PageSize: 2),
            CancellationToken.None);
        var page3 = await NewHandler(db).Handle(
            new ListBlogCommentsQuery(blog.Id, Page: 3, PageSize: 2),
            CancellationToken.None);

        page1.Value!.Items.Should().HaveCount(2);
        page2.Value!.Items.Should().HaveCount(2);
        page3.Value!.Items.Should().HaveCount(1);
        page1.Value.TotalCount.Should().Be(5);
        page2.Value.TotalCount.Should().Be(5);
        page3.Value.TotalCount.Should().Be(5);
    }

    // ── Cache-key wiring (matches existing BlogReadQueryTests pattern) ────────

    [Fact]
    public void ListBlogCommentsQuery_ImplementsICacheableQuery()
    {
        typeof(ICacheableQuery).IsAssignableFrom(typeof(ListBlogCommentsQuery))
            .Should().BeTrue();
    }

    [Fact]
    public void ListBlogCommentsQuery_UsesContentBlogsCacheKeys()
    {
        var blogId = Guid.NewGuid();
        var q = new ListBlogCommentsQuery(blogId, Page: 2, PageSize: 25);

        q.CacheKey.Should().Be(ContentBlogsCacheKeys.BlogComments(blogId, 2, 25));
        q.Tags.Should().ContainSingle()
            .Which.Should().Be(ContentBlogsCacheKeys.BlogCommentsTag(blogId));
    }

    [Fact]
    public void ListBlogCommentsQuery_CacheKey_DiffersByPageAndSize()
    {
        var blogId = Guid.NewGuid();
        var a = new ListBlogCommentsQuery(blogId, Page: 1, PageSize: 20);
        var b = new ListBlogCommentsQuery(blogId, Page: 2, PageSize: 20);
        var c = new ListBlogCommentsQuery(blogId, Page: 1, PageSize: 50);

        new[] { a.CacheKey, b.CacheKey, c.CacheKey }.Distinct().Should().HaveCount(3);
    }

    // ── BlogCommentDto safety: must not expose RowVersion or audit metadata ──

    [Fact]
    public void BlogCommentDto_DoesNotExposeRowVersion_OrSoftDeleteFields()
    {
        var props = typeof(global::ContentBlogs.Application.Queries.BlogComment.Dtos.BlogCommentDto)
            .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();

        var forbidden = new[] { "RowVersion", "IsDeleted", "DeletedAt", "CreatedByUserId" };
        foreach (var name in forbidden)
        {
            props.Should().NotContain(name,
                $"BlogCommentDto must not expose '{name}' to public callers");
        }
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static ContentBlogsDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-list-comments-{Guid.NewGuid():N}")
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

    private static ListBlogCommentsQueryHandler NewHandler(ContentBlogsDbContext db) =>
        new(
            blogRepository:        new BlogRepository(db),
            blogCommentRepository: new BlogCommentRepository(db),
            logger:                NullLogger<ListBlogCommentsQueryHandler>.Instance);
}
