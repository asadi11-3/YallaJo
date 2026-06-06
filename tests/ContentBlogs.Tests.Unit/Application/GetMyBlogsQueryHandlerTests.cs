using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Queries.Blog.GetMyBlogs;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Infrastructure.Persistence;
using ContentBlogs.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Creator Backend Contract Polish (Gap 1): GET /my-blogs must return the article's
/// real <c>Status</c> and a resolved <c>SourceLanguageCode</c> (no more hardcoded
/// "default"), so the creator's My Articles list shows truthful data.
/// </summary>
public sealed class GetMyBlogsQueryHandlerTests
{
    private static readonly Guid EnglishLanguageId = Guid.NewGuid();
    private static readonly Guid ArabicLanguageId  = Guid.NewGuid();

    private const string ValidContent =
        "Petra is one of the most famous archaeological sites in the world, carved " +
        "into rose-coloured sandstone cliffs in the southern Jordanian desert.";

    private static ContentBlogsDbContext NewDb() =>
        new(new DbContextOptionsBuilder<ContentBlogsDbContext>()
            .UseInMemoryDatabase($"content-blogs-my-blogs-{Guid.NewGuid():N}")
            .Options);

    private static IActiveLanguageProvider Languages()
    {
        var p = Substitute.For<IActiveLanguageProvider>();
        p.GetActiveLanguagesAsync(Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new ActiveLanguage(EnglishLanguageId, "en"),
                new ActiveLanguage(ArabicLanguageId, "ar"),
            });
        return p;
    }

    private static HybridCache RealCache() =>
        new ServiceCollection().AddHybridCache().Services
            .BuildServiceProvider()
            .GetRequiredService<HybridCache>();

    private static GetMyBlogsQueryHandler NewHandler(ContentBlogsDbContext db, Guid userId)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns<Guid?>(userId);
        return new GetMyBlogsQueryHandler(
            new BlogRepository(db),
            currentUser,
            Languages(),
            RealCache(),
            NullLogger<GetMyBlogsQueryHandler>.Instance);
    }

    [Fact]
    public async Task Returns_real_status_and_source_language_code()
    {
        await using var db = NewDb();
        var authorId = Guid.NewGuid();

        // A draft article in English.
        var draft = Blog.Create(
            title: "My Draft", slug: "my-draft", content: ValidContent,
            authorId: authorId, sourceLanguageId: EnglishLanguageId, utcNow: DateTime.UtcNow);
        db.Blogs.Add(draft);
        await db.SaveChangesAsync();

        var handler = NewHandler(db, authorId);

        var result = await handler.Handle(
            new GetMyBlogsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Items.Should().ContainSingle().Subject;

        item.Status.Should().Be("Draft", "Gap 1: the real BlogStatus is now projected");
        item.SourceLanguageCode.Should().Be("en", "Gap 1: source language is resolved, not 'default'");
    }

    [Fact]
    public async Task Reflects_published_status_after_publish()
    {
        await using var db = NewDb();
        var authorId = Guid.NewGuid();

        var blog = Blog.Create(
            title: "Petra", slug: "petra", content: ValidContent,
            authorId: authorId, sourceLanguageId: ArabicLanguageId, utcNow: DateTime.UtcNow);
        blog.Publish(DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewHandler(db, authorId);

        var result = await handler.Handle(
            new GetMyBlogsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        var item = result.Value!.Items.Should().ContainSingle().Subject;
        item.Status.Should().Be("Published");
        item.SourceLanguageCode.Should().Be("ar");
    }

    [Fact]
    public async Task Unknown_language_yields_null_source_language_code()
    {
        await using var db = NewDb();
        var authorId = Guid.NewGuid();

        // Source language not in the active set → code cannot be resolved.
        var blog = Blog.Create(
            title: "Mystery", slug: "mystery", content: ValidContent,
            authorId: authorId, sourceLanguageId: Guid.NewGuid(), utcNow: DateTime.UtcNow);
        db.Blogs.Add(blog);
        await db.SaveChangesAsync();

        var handler = NewHandler(db, authorId);

        var result = await handler.Handle(
            new GetMyBlogsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        var item = result.Value!.Items.Should().ContainSingle().Subject;
        item.Status.Should().Be("Draft");
        item.SourceLanguageCode.Should().BeNull();
    }

    // ── Cache-key correctness (status must not collide) ─────────────────────────

    [Fact]
    public void Cache_key_differs_by_status_filter()
    {
        var userId = Guid.NewGuid();

        var all       = ContentBlogsCacheKeys.MyBlogs(userId, 1, 20, status: null);
        var draft     = ContentBlogsCacheKeys.MyBlogs(userId, 1, 20, status: "Draft");
        var published = ContentBlogsCacheKeys.MyBlogs(userId, 1, 20, status: "Published");

        all.Should().NotBe(draft);
        draft.Should().NotBe(published);
        all.Should().NotBe(published);

        // Status is case-insensitive in the effective filter → same key.
        ContentBlogsCacheKeys.MyBlogs(userId, 1, 20, "draft")
            .Should().Be(draft);

        // Page / pageSize still differentiate keys.
        ContentBlogsCacheKeys.MyBlogs(userId, 2, 20, "Draft").Should().NotBe(draft);
        ContentBlogsCacheKeys.MyBlogs(userId, 1, 50, "Draft").Should().NotBe(draft);
    }

    [Fact]
    public async Task Different_status_filters_do_not_return_each_others_cached_results()
    {
        await using var db = NewDb();
        var authorId = Guid.NewGuid();

        // One Draft + one Published article for the same author.
        var draft = Blog.Create(
            title: "Draft One", slug: "draft-one", content: ValidContent,
            authorId: authorId, sourceLanguageId: EnglishLanguageId, utcNow: DateTime.UtcNow);
        var published = Blog.Create(
            title: "Published One", slug: "published-one", content: ValidContent,
            authorId: authorId, sourceLanguageId: EnglishLanguageId, utcNow: DateTime.UtcNow);
        published.Publish(DateTime.UtcNow);
        db.Blogs.AddRange(draft, published);
        await db.SaveChangesAsync();

        // Reuse ONE handler (hence one shared cache) across both filtered queries to prove
        // the status-aware key prevents cross-filter cache collisions.
        var handler = NewHandler(db, authorId);

        var draftResult = await handler.Handle(
            new GetMyBlogsQuery(Page: 1, PageSize: 20, StatusFilter: "Draft"), CancellationToken.None);
        var publishedResult = await handler.Handle(
            new GetMyBlogsQuery(Page: 1, PageSize: 20, StatusFilter: "Published"), CancellationToken.None);

        draftResult.Value!.Items.Should().ContainSingle()
            .Which.Status.Should().Be("Draft");
        publishedResult.Value!.Items.Should().ContainSingle()
            .Which.Status.Should().Be("Published");
    }
}
