using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.Persistence.Configurations;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ContentBlogs.Tests.Unit.Infrastructure;

internal sealed class FeaturedBlogRelationalTestContext(
    DbContextOptions<FeaturedBlogRelationalTestContext> options)
    : DbContext(options)
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<BlogTranslation> BlogTranslations => Set<BlogTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new BlogConfiguration().Configure(modelBuilder.Entity<Blog>());
        new BlogTranslationConfiguration().Configure(modelBuilder.Entity<BlogTranslation>());

        // Ignore navigations/entities not under test so EF does not require their
        // configurations (BlogTour/BlogComment key + SQL Server column types). The
        // featured-query predicates only touch Blog scalar columns and BlogTranslation.
        modelBuilder.Ignore<BlogTour>();
        modelBuilder.Ignore<BlogComment>();

        // SQLite compatibility shims (test-only; do not change filter/translation semantics):
        //  - nvarchar(max) → TEXT so EnsureCreated() can build the schema.
        //  - [Timestamp] RowVersion → ValueGeneratedNever so INSERTs include the default
        //    value instead of expecting a DB-generated rowversion (SQLite has none).
        modelBuilder.Entity<Blog>().Property(x => x.Content).HasColumnType("TEXT");
        modelBuilder.Entity<Blog>().Property(x => x.RowVersion).ValueGeneratedNever();
        modelBuilder.Entity<BlogTranslation>().Property(x => x.Content).HasColumnType("TEXT");

        base.OnModelCreating(modelBuilder);
    }
}

// ── CB-04 relational regression tests ─────────────────────────────────────────

/// <summary>
/// CB-04 regression: <c>Blog.IsFeatured</c> is <c>[NotMapped]</c> and cannot be
/// translated by relational providers. These tests run the exact production
/// predicates (from <c>BlogRepository.GetFeaturedBlogInPlaceScopeAsync</c> and the
/// <c>ListBlogsQueryHandler</c> filter) against SQLite and assert they translate
/// and execute WITHOUT throwing — which the previous <c>blog.IsFeatured</c>
/// predicates would not (they throw <see cref="System.InvalidOperationException"/>
/// "could not be translated" on a relational provider).
///
/// EF Core InMemory cannot catch this class of bug because it evaluates LINQ
/// in-process; SQLite forces real SQL translation.
/// </summary>
public sealed class FeaturedBlogRelationalQueryTests : IDisposable
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, carved " +
        "into rose-coloured sandstone cliffs in the southern Jordanian desert.";

    private readonly FeaturedBlogRelationalTestContext _ctx;
    private readonly SqliteConnection _conn;

    public FeaturedBlogRelationalQueryTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();

        var opts = new DbContextOptionsBuilder<FeaturedBlogRelationalTestContext>()
            .UseSqlite(_conn)
            .Options;

        _ctx = new FeaturedBlogRelationalTestContext(opts);
        _ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _conn.Dispose();
    }

    private static Blog NewPublishedBlog(string slug, Guid? placeId = null)
    {
        var blog = Blog.Create(
            title: $"Blog {slug}",
            slug: slug,
            content: ValidContent,
            authorId: Guid.NewGuid(),
            sourceLanguageId: EnglishLanguageId,
            utcNow: DateTime.UtcNow,
            placeId: placeId);
        blog.Publish(DateTime.UtcNow);
        return blog;
    }

    // Production predicate copy: ListBlogsQueryHandler filter (CB-04 form, no IsFeatured).
    private async Task<List<Blog>> RunListBlogsFilterAsync(bool? featuredFilter, Guid? placeId = null)
    {
        var now = DateTime.UtcNow;
        return await _ctx.Blogs
            .AsNoTracking()
            .Where(blog => blog.Status == BlogStatus.Published
                && (placeId == null || blog.PlaceId == placeId)
                && (featuredFilter == null
                    || (featuredFilter == true
                        ? (blog.FeaturedAt != null
                            && (blog.FeaturedUntil == null || blog.FeaturedUntil > now))
                        : !(blog.FeaturedAt != null
                            && (blog.FeaturedUntil == null || blog.FeaturedUntil > now)))))
            .OrderByDescending(b => b.PublishedAt)
            .ToListAsync();
    }

    // Production predicate copy: BlogRepository.GetFeaturedBlogInPlaceScopeAsync (CB-04 form).
    private async Task<Blog?> RunFeaturedInPlaceScopeAsync(Guid? placeId, Guid? excludeBlogId = null)
    {
        var now = DateTime.UtcNow;
        return await _ctx.Blogs
            .AsNoTracking()
            .Where(blog => blog.FeaturedAt != null
                && (blog.FeaturedUntil == null || blog.FeaturedUntil > now)
                && blog.PlaceId == placeId
                && (excludeBlogId == null || blog.Id != excludeBlogId.Value))
            .FirstOrDefaultAsync();
    }

    // ── ListBlogs filter ────────────────────────────────────────────────────

    [Fact]
    public async Task ListBlogs_WithoutFeaturedFilter_ReturnsAll_AndDoesNotThrow()
    {
        var featured = NewPublishedBlog("featured");
        featured.Feature(Guid.Empty, DateTime.UtcNow, until: DateTime.UtcNow.AddMinutes(30));
        var ordinary = NewPublishedBlog("ordinary");
        _ctx.Blogs.AddRange(featured, ordinary);
        await _ctx.SaveChangesAsync();

        var result = await RunListBlogsFilterAsync(featuredFilter: null);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListBlogs_WithFeaturedTrue_ReturnsOnlyFeatured()
    {
        var featured = NewPublishedBlog("featured");
        featured.Feature(Guid.Empty, DateTime.UtcNow, until: DateTime.UtcNow.AddMinutes(30));
        var ordinary = NewPublishedBlog("ordinary");
        _ctx.Blogs.AddRange(featured, ordinary);
        await _ctx.SaveChangesAsync();

        var result = await RunListBlogsFilterAsync(featuredFilter: true);

        result.Should().ContainSingle().Which.Slug.Should().Be("featured");
    }

    [Fact]
    public async Task ListBlogs_WithFeaturedFalse_ReturnsOnlyNonFeatured()
    {
        var featured = NewPublishedBlog("featured");
        featured.Feature(Guid.Empty, DateTime.UtcNow, until: DateTime.UtcNow.AddMinutes(30));
        var ordinary1 = NewPublishedBlog("ordinary-1");
        var ordinary2 = NewPublishedBlog("ordinary-2");
        _ctx.Blogs.AddRange(featured, ordinary1, ordinary2);
        await _ctx.SaveChangesAsync();

        var result = await RunListBlogsFilterAsync(featuredFilter: false);

        result.Should().HaveCount(2);
        result.Select(b => b.Slug).Should().BeEquivalentTo(["ordinary-1", "ordinary-2"]);
    }

    [Fact]
    public async Task ListBlogs_WithFeaturedFalse_TreatsExpiredFeaturedAsNonFeatured()
    {
        // A blog whose FeaturedUntil is in the past is no longer "featured" and must
        // appear in the IsFeatured=false result set.
        var expired = NewPublishedBlog("expired-featured");
        // FeaturedAt in the past, FeaturedUntil already elapsed → currently not featured.
        expired.Feature(Guid.Empty, DateTime.UtcNow.AddHours(-2), until: DateTime.UtcNow.AddMinutes(-5));
        _ctx.Blogs.Add(expired);
        await _ctx.SaveChangesAsync();

        var featuredOnly = await RunListBlogsFilterAsync(featuredFilter: true);
        var nonFeatured = await RunListBlogsFilterAsync(featuredFilter: false);

        featuredOnly.Should().BeEmpty();
        nonFeatured.Should().ContainSingle().Which.Slug.Should().Be("expired-featured");
    }

    // ── GetFeaturedBlogInPlaceScopeAsync ──────────────────────────────────────

    [Fact]
    public async Task FeaturedInPlaceScope_ReturnsActiveFeaturedBlog()
    {
        var placeId = Guid.NewGuid();
        var featured = NewPublishedBlog("featured-in-place", placeId);
        featured.Feature(Guid.Empty, DateTime.UtcNow, until: DateTime.UtcNow.AddMinutes(30));
        _ctx.Blogs.Add(featured);
        await _ctx.SaveChangesAsync();

        var result = await RunFeaturedInPlaceScopeAsync(placeId);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("featured-in-place");
    }

    [Fact]
    public async Task FeaturedInPlaceScope_IgnoresExpiredFeaturedBlog()
    {
        var placeId = Guid.NewGuid();
        var expired = NewPublishedBlog("expired-in-place", placeId);
        expired.Feature(Guid.Empty, DateTime.UtcNow.AddHours(-2), until: DateTime.UtcNow.AddMinutes(-5)); // expired
        _ctx.Blogs.Add(expired);
        await _ctx.SaveChangesAsync();

        var result = await RunFeaturedInPlaceScopeAsync(placeId);

        result.Should().BeNull("an expired featured blog is no longer 'currently featured'");
    }

    [Fact]
    public async Task FeaturedInPlaceScope_WithIndefiniteFeature_IsReturned()
    {
        // FeaturedUntil == null means featured indefinitely → must be returned.
        var placeId = Guid.NewGuid();
        var indefinite = NewPublishedBlog("indefinite-in-place", placeId);
        indefinite.Feature(Guid.Empty, DateTime.UtcNow, until: null);
        _ctx.Blogs.Add(indefinite);
        await _ctx.SaveChangesAsync();

        var result = await RunFeaturedInPlaceScopeAsync(placeId);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("indefinite-in-place");
    }
}
