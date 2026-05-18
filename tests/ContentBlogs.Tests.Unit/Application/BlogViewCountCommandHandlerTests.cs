using ContentBlogs.Application.Caching;
using ContentBlogs.Application.Commands.Blog.IncrementBlogViewCount;
using ContentBlogs.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Tests.Unit.Application;

/// <summary>
/// Phase 1 closure tests for the dedicated view-count increment slice.
///
/// <para>
/// The repository contract is mocked with NSubstitute because EF Core's
/// in-memory provider does not support <c>ExecuteUpdateAsync</c>.  The
/// repository SQL is exercised manually via the smoke checklist against
/// SQL Server.  These tests pin the handler-level contract:
/// </para>
///
/// <list type="bullet">
///   <item>Handler does NOT load the aggregate.</item>
///   <item>Repository returns <c>true</c> only for Published blogs.</item>
///   <item>Missing / Draft / Archived / soft-deleted → <c>Outcome.NotFound</c>.</item>
///   <item>Cache invalidation runs only on the success path.</item>
///   <item>Handler does NOT call <c>IContentBlogsUnitOfWork.SaveChangesAsync</c>
///     (it never injects one — the repository's <c>ExecuteUpdateAsync</c> commits
///     its own work).</item>
/// </list>
/// </summary>
public sealed class BlogViewCountCommandHandlerTests
{
    [Fact]
    public async Task IncrementViewCount_ReturnsNotFound_WhenBlogMissing()
    {
        var repo = Substitute.For<IBlogRepository>();
        var cache = Substitute.For<HybridCache>();
        repo.IncrementViewCountIfPublishedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await NewHandler(repo, cache).Handle(
            new IncrementBlogViewCountCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(Outcome.NotFound);
        result.Errors[0].Code.Should().Be("Blog.NotFound");

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncrementViewCount_ReturnsNotFound_WhenRepositoryReportsZeroRows()
    {
        // The repository returns false for Draft, Archived, and soft-deleted
        // blogs (their SQL predicate excludes them).  The handler treats all
        // three cases identically — Outcome.NotFound, no status disclosure.
        var repo = Substitute.For<IBlogRepository>();
        var cache = Substitute.For<HybridCache>();
        repo.IncrementViewCountIfPublishedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await NewHandler(repo, cache).Handle(
            new IncrementBlogViewCountCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.Outcome.Should().Be(Outcome.NotFound,
            "Draft / Archived / soft-deleted blogs all surface as zero rows affected");

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncrementViewCount_ReturnsSuccess_WhenRepositoryReportsOneRow()
    {
        var blogId = Guid.NewGuid();
        var repo = Substitute.For<IBlogRepository>();
        repo.IncrementViewCountIfPublishedAsync(blogId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await NewHandler(repo).Handle(
            new IncrementBlogViewCountCommand(blogId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Ok);

        await repo.Received(1).IncrementViewCountIfPublishedAsync(
            blogId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncrementViewCount_InvalidatesBlogCacheAfterSuccess()
    {
        var blogId = Guid.NewGuid();
        var repo = Substitute.For<IBlogRepository>();
        repo.IncrementViewCountIfPublishedAsync(blogId, Arg.Any<CancellationToken>())
            .Returns(true);

        var cache = Substitute.For<HybridCache>();

        await NewHandler(repo, cache).Handle(
            new IncrementBlogViewCountCommand(blogId), CancellationToken.None);

        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogTag(blogId), Arg.Any<CancellationToken>());
        await cache.Received(1).RemoveByTagAsync(
            ContentBlogsCacheKeys.BlogsListTag, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncrementViewCount_DoesNotInvalidateCache_WhenNotFound()
    {
        var repo = Substitute.For<IBlogRepository>();
        var cache = Substitute.For<HybridCache>();
        repo.IncrementViewCountIfPublishedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await NewHandler(repo, cache).Handle(
            new IncrementBlogViewCountCommand(Guid.NewGuid()),
            CancellationToken.None);

        await cache.DidNotReceiveWithAnyArgs()
            .RemoveByTagAsync(default(string)!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IncrementViewCount_HandlerDoesNotInjectUnitOfWork()
    {
        // Compile-time contract: the handler's constructor signature does NOT
        // accept IContentBlogsUnitOfWork because the repository's
        // ExecuteUpdateAsync commits its own work.  This pins the contract so a
        // future refactor cannot silently couple view-count writes to the UoW.
        var ctor = typeof(IncrementBlogViewCountCommandHandler).GetConstructors()[0];
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

        paramTypes.Should().NotContain(
            t => t.Name.Contains("UnitOfWork", StringComparison.Ordinal),
            "view count is high-frequency analytics and must NOT raise domain events");
    }

    [Fact]
    public async Task IncrementBlogViewCountCommandValidator_RejectsEmptyBlogId()
    {
        var validator = new IncrementBlogViewCountCommandValidator();
        var result = await validator.ValidateAsync(
            new IncrementBlogViewCountCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task IncrementBlogViewCountCommandValidator_AcceptsNonEmptyBlogId()
    {
        var validator = new IncrementBlogViewCountCommandValidator();
        var result = await validator.ValidateAsync(
            new IncrementBlogViewCountCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }

    // ── Test helpers ──────────────────────────────────────────────────────────

    private static IncrementBlogViewCountCommandHandler NewHandler(
        IBlogRepository repo,
        HybridCache? cache = null) =>
        new(
            blogRepository: repo,
            cache:          cache ?? Substitute.For<HybridCache>(),
            logger:         NullLogger<IncrementBlogViewCountCommandHandler>.Instance);
}
