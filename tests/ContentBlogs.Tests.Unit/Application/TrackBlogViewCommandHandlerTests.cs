using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.TrackBlogView;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Handler-level contract tests for <see cref="TrackBlogViewCommandHandler"/>.
/// <para>
/// The <c>IBlogViewCounter</c> is mocked because EF Core's in-memory provider
/// does not support <c>ExecuteUpdateAsync</c> or explicit transactions — the
/// SQL flow is exercised manually via the smoke checklist against SQL Server.
/// These tests pin the handler-level contract:
/// </para>
/// <list type="bullet">
///   <item>Handler hashes via <c>IBlogViewerHashService</c> before delegating.</item>
///   <item>Counter returns <c>(Slug: null)</c> for missing / Draft / Archived /
///     soft-deleted blogs — all surface as <c>Outcome.NotFound</c>.</item>
///   <item>Cache invalidation runs ONLY on the <c>Counted = true</c> success path.</item>
///   <item>Repeat view (Counted = false) returns Success and invalidates NOTHING.</item>
///   <item>Handler does NOT call <c>IContentBlogsUnitOfWork.SaveChangesAsync</c>
///     (it never injects one — the counter service commits its own work).</item>
/// </list>
/// </summary>
public sealed class TrackBlogViewCommandHandlerTests
{
    private static readonly byte[] FakeHash = new byte[32];

    [Fact]
    public async Task TrackBlogView_ReturnsNotFound_WhenBlogMissing()
    {
        var counter = Substitute.For<IBlogViewCounter>();
        var hashService = StubHashService();
        var cache = Substitute.For<HybridCache>();
        counter.TryCountAsync(Arg.Any<Guid>(), Arg.Any<BlogViewerKind>(),
                Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new BlogViewCountResult(Counted: false, ViewCount: null, Slug: null));

        var result = await NewHandler(counter, hashService, cache).Handle(
            NewCommand(),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.NotFound");

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackBlogView_ReturnsNotFound_WhenCounterReportsSlugNull()
    {
        // Counter returns (Counted: false, ViewCount: null, Slug: null) for
        // Draft / Archived / soft-deleted blogs (the SQL predicate excludes them).
        // The handler treats all three cases identically — Outcome.NotFound,
        // no status disclosure.
        var counter = Substitute.For<IBlogViewCounter>();
        counter.TryCountAsync(Arg.Any<Guid>(), Arg.Any<BlogViewerKind>(),
                Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new BlogViewCountResult(Counted: false, ViewCount: null, Slug: null));

        var cache = Substitute.For<HybridCache>();
        var result = await NewHandler(counter, cache: cache).Handle(
            NewCommand(),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound,
            "Draft / Archived / soft-deleted blogs all surface as Slug=null");

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackBlogView_ReturnsSuccessCountedTrue_OnFirstView()
    {
        var blogId = Guid.NewGuid();
        var counter = Substitute.For<IBlogViewCounter>();
        counter.TryCountAsync(blogId, Arg.Any<BlogViewerKind>(),
                Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new BlogViewCountResult(
                Counted: true, ViewCount: 1, Slug: "petra-sunrise"));

        var result = await NewHandler(counter).Handle(
            new TrackBlogViewCommand(blogId, BlogViewerKind.Anonymous, "anon-token"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);
        result.Value.Should().NotBeNull();
        result.Value!.Counted.Should().BeTrue();
        result.Value.ViewCount.Should().Be(1);
        result.Value.Slug.Should().Be("petra-sunrise");
    }

    [Fact]
    public async Task TrackBlogView_ReturnsSuccessCountedFalse_OnRepeatView()
    {
        // Lifetime-unique: same viewer + same blog → counter reports
        // Counted=false with the current ViewCount + slug.  Handler must
        // surface this as Success (not NotFound) and MUST NOT invalidate cache.
        var blogId = Guid.NewGuid();
        var counter = Substitute.For<IBlogViewCounter>();
        counter.TryCountAsync(blogId, Arg.Any<BlogViewerKind>(),
                Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new BlogViewCountResult(
                Counted: false, ViewCount: 42, Slug: "petra-sunrise"));

        var cache = Substitute.For<HybridCache>();
        var result = await NewHandler(counter, cache: cache).Handle(
            new TrackBlogViewCommand(blogId, BlogViewerKind.Authenticated, "user-id"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Counted.Should().BeFalse();
        result.Value.ViewCount.Should().Be(42);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackBlogView_InvalidatesCache_OnCountedTrue()
    {
        // Counted=true must evict BlogTag(id), BlogSlugTag(slug), and BlogsListTag.
        var blogId = Guid.NewGuid();
        const string slug = "petra-sunrise";
        var counter = Substitute.For<IBlogViewCounter>();
        counter.TryCountAsync(blogId, Arg.Any<BlogViewerKind>(),
                Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new BlogViewCountResult(Counted: true, ViewCount: 5, Slug: slug));

        var cache = Substitute.For<HybridCache>();
        await NewHandler(counter, cache: cache).Handle(
            new TrackBlogViewCommand(blogId, BlogViewerKind.Anonymous, "tok"),
            CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blogId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogSlugTag(slug), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackBlogView_DoesNotInvalidateCache_OnCountedFalse()
    {
        var counter = Substitute.For<IBlogViewCounter>();
        counter.TryCountAsync(Arg.Any<Guid>(), Arg.Any<BlogViewerKind>(),
                Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new BlogViewCountResult(
                Counted: false, ViewCount: 7, Slug: "petra-sunrise"));

        var cache = Substitute.For<HybridCache>();
        await NewHandler(counter, cache: cache).Handle(
            NewCommand(),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackBlogView_DoesNotInvalidateCache_OnNotFound()
    {
        var counter = Substitute.For<IBlogViewCounter>();
        counter.TryCountAsync(Arg.Any<Guid>(), Arg.Any<BlogViewerKind>(),
                Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new BlogViewCountResult(Counted: false, ViewCount: null, Slug: null));

        var cache = Substitute.For<HybridCache>();
        await NewHandler(counter, cache: cache).Handle(
            NewCommand(),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TrackBlogView_PassesViewerKindAndHashToCounter()
    {
        // Pins the wiring: handler must call IBlogViewerHashService and
        // forward the resulting bytes (not the raw viewerId) to the counter.
        var blogId = Guid.NewGuid();
        var counter = Substitute.For<IBlogViewCounter>();
        counter.TryCountAsync(Arg.Any<Guid>(), Arg.Any<BlogViewerKind>(),
                Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(new BlogViewCountResult(Counted: true, ViewCount: 1, Slug: "s"));

        var hashService = Substitute.For<IBlogViewerHashService>();
        var sentinel = new byte[32]; sentinel[0] = 0xAB;
        hashService.Hash(BlogViewerKind.Authenticated, "u-1")
            .Returns(sentinel);

        await NewHandler(counter, hashService).Handle(
            new TrackBlogViewCommand(blogId, BlogViewerKind.Authenticated, "u-1"),
            CancellationToken.None);

        hashService.Received(1).Hash(BlogViewerKind.Authenticated, "u-1");
        await counter.Received(1).TryCountAsync(
            blogId,
            BlogViewerKind.Authenticated,
            Arg.Is<byte[]>(b => b.Length == 32 && b[0] == 0xAB),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void TrackBlogView_HandlerDoesNotInjectUnitOfWork()
    {
        // Compile-time contract regression carried over from
        // CONTENTBLOGS-VIEW-DEBOUNCE-IMPL-001 — view counting is high-frequency
        // analytics and the counter service commits its own work.
        var ctor = typeof(TrackBlogViewCommandHandler).GetConstructors()[0];
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

        paramTypes.Should().NotContain(
            t => t.Name.Contains("UnitOfWork", StringComparison.Ordinal),
            "view count is high-frequency analytics and must NOT couple to UoW or raise domain events");
    }

    [Fact]
    public async Task TrackBlogViewCommandValidator_RejectsEmptyBlogId()
    {
        var validator = new TrackBlogViewCommandValidator();
        var result = await validator.ValidateAsync(
            new TrackBlogViewCommand(Guid.Empty, BlogViewerKind.Anonymous, "tok"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TrackBlogViewCommandValidator_RejectsEmptyViewerId()
    {
        var validator = new TrackBlogViewCommandValidator();
        var result = await validator.ValidateAsync(
            new TrackBlogViewCommand(Guid.NewGuid(), BlogViewerKind.Anonymous, string.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task TrackBlogViewCommandValidator_AcceptsValidCommand()
    {
        var validator = new TrackBlogViewCommandValidator();
        var result = await validator.ValidateAsync(
            new TrackBlogViewCommand(Guid.NewGuid(), BlogViewerKind.Authenticated, "user-id-123"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task TrackBlogViewCommandValidator_RejectsInvalidViewerKindEnum()
    {
        // Out-of-range enum value must fail the IsInEnum() rule.
        var validator = new TrackBlogViewCommandValidator();
        var result = await validator.ValidateAsync(
            new TrackBlogViewCommand(Guid.NewGuid(), (BlogViewerKind)99, "tok"));

        result.IsValid.Should().BeFalse();
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static TrackBlogViewCommand NewCommand() =>
        new(Guid.NewGuid(), BlogViewerKind.Anonymous, "anon-token");

    private static IBlogViewerHashService StubHashService()
    {
        var s = Substitute.For<IBlogViewerHashService>();
        s.Hash(Arg.Any<BlogViewerKind>(), Arg.Any<string>()).Returns(FakeHash);
        return s;
    }

    private static TrackBlogViewCommandHandler NewHandler(
        IBlogViewCounter counter,
        IBlogViewerHashService? hashService = null,
        HybridCache? cache = null) =>
        new(
            counter:     counter,
            hashService: hashService ?? StubHashService(),
            cache:       cache ?? Substitute.For<HybridCache>(),
            logger:      NullLogger<TrackBlogViewCommandHandler>.Instance);
}
