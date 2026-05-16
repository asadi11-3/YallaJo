using ContentBlogs.Domain.Entities;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ContentBlogs.Tests.Unit.Infrastructure;

public sealed class BlogRepositoryTests
{
    [Fact]
    public async Task BlogRepository_IsSlugReservedAsync_ReturnsTrue_ForExistingSlug()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Blogs.Add(CreateBlog("petra-guide"));
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var result = await repository.IsSlugReservedAsync("petra-guide", excludeId: null);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task BlogRepository_IsSlugReservedAsync_ReturnsFalse_ForMissingSlug()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Blogs.Add(CreateBlog("petra-guide"));
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var result = await repository.IsSlugReservedAsync("wadi-rum", excludeId: null);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task BlogRepository_IsSlugReservedAsync_ExcludesOwnSlug_WhenExcludeIdProvided()
    {
        await using var dbContext = CreateDbContext();
        var blog = CreateBlog("petra-guide");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var result = await repository.IsSlugReservedAsync("petra-guide", excludeId: blog.Id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task BlogRepository_IsSlugReservedAsync_UsesQueryFilter_ForSoftDeletedBlog()
    {
        await using var dbContext = CreateDbContext();
        var deletedBlog = CreateBlog("petra-guide");
        deletedBlog.Delete(UtcNow.AddMinutes(1));

        dbContext.Blogs.Add(deletedBlog);
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var result = await repository.IsSlugReservedAsync("petra-guide", excludeId: null);

        result.Should().BeFalse("soft-deleted blogs are excluded by the global query filter");
    }

    [Fact]
    public async Task BlogRepository_GetBySlugAsync_ReturnsBlog_ForExistingSlug()
    {
        await using var dbContext = CreateDbContext();
        var blog = CreateBlog("petra-guide");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var found = await repository.GetBySlugAsync("petra-guide");

        found.Should().NotBeNull();
        found!.Id.Should().Be(blog.Id);
    }

    [Fact]
    public async Task BlogRepository_GetBySlugAsync_ReturnsNull_ForSoftDeletedBlog()
    {
        await using var dbContext = CreateDbContext();
        var deletedBlog = CreateBlog("petra-guide");
        deletedBlog.Delete(UtcNow.AddMinutes(1));

        dbContext.Blogs.Add(deletedBlog);
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var found = await repository.GetBySlugAsync("petra-guide");

        found.Should().BeNull();
    }

    [Fact]
    public async Task BlogRepository_NormalizesSlugInput()
    {
        await using var dbContext = CreateDbContext();
        var blog = CreateBlog("petra-guide");
        dbContext.Blogs.Add(blog);
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var isReserved = await repository.IsSlugReservedAsync("  PETRA-GUIDE  ", excludeId: null);
        var found = await repository.GetBySlugAsync("  PETRA-GUIDE  ");

        isReserved.Should().BeTrue();
        found.Should().NotBeNull();
        found!.Id.Should().Be(blog.Id);
    }

    // ── GetActiveByPlaceIdAsync (CONTENTBLOGS-PLACE-DELETED-CONSUMER-IMPL-001) ─

    [Fact]
    public async Task BlogRepository_GetActiveByPlaceIdAsync_ReturnsOnlyMatchingNonDeletedBlogs()
    {
        await using var dbContext = CreateDbContext();
        var placeA = Guid.NewGuid();
        var placeB = Guid.NewGuid();
        var a1 = CreateBlog("a1", placeId: placeA);
        var a2 = CreateBlog("a2", placeId: placeA);
        var b1 = CreateBlog("b1", placeId: placeB);
        var orphan = CreateBlog("orphan"); // PlaceId == null
        dbContext.Blogs.AddRange(a1, a2, b1, orphan);
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var found = await repository.GetActiveByPlaceIdAsync(placeA);

        found.Should().HaveCount(2);
        found.Select(b => b.Slug).Should().BeEquivalentTo(["a1", "a2"]);
    }

    [Fact]
    public async Task BlogRepository_GetActiveByPlaceIdAsync_DoesNotReturnDeletedBlogs()
    {
        await using var dbContext = CreateDbContext();
        var placeA = Guid.NewGuid();
        var liveBlog = CreateBlog("live", placeId: placeA);
        var deletedBlog = CreateBlog("deleted", placeId: placeA);
        deletedBlog.Delete(UtcNow.AddMinutes(1));
        dbContext.Blogs.AddRange(liveBlog, deletedBlog);
        await dbContext.SaveChangesAsync();

        var repository = new BlogRepository(dbContext);

        var found = await repository.GetActiveByPlaceIdAsync(placeA);

        found.Should().ContainSingle()
            .Which.Slug.Should().Be("live",
                "soft-deleted blogs are excluded by the global !IsDeleted query filter " +
                "— GetActiveByPlaceIdAsync must NOT use IgnoreQueryFilters");
    }

    private static ContentBlogsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-repo-tests-{Guid.NewGuid():N}")
            .Options;

        return new ContentBlogsDbContext(options);
    }

    private static Blog CreateBlog(string slug, Guid? placeId = null)
    {
        return Blog.Create(
            title: "Petra Guide",
            slug: slug,
            content: new string('x', 200),
            authorId: Guid.NewGuid(),
            sourceLanguageId: Guid.NewGuid(),
            utcNow: UtcNow,
            placeId: placeId);
    }

    private static readonly DateTime UtcNow =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
