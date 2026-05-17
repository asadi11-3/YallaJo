
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.Configuration;
using ContentBlogs.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ContentBlogs.Tests.Unit.Infrastructure;

public sealed class BlogViewerHashServiceTests
{
    private const string TestSecret = "YallaJo-Test-Secret-NotProduction-2026";

    [Fact]
    public void Hash_IsStable_ForSameInputs()
    {
        var service = NewService();

        var first = service.Hash(BlogViewerKind.Authenticated, "user-1");
        var second = service.Hash(BlogViewerKind.Authenticated, "user-1");

        first.Should().BeEquivalentTo(second,
            "HMAC-SHA256 is deterministic for the same key + input");
    }

    [Fact]
    public void Hash_DiffersBetween_AuthenticatedAndAnonymous_ForSameId()
    {
        // Different "label" (user vs anon) prevents collision between an
        // authenticated user whose UserId.ToString() happens to match an
        // anonymous viewer's cookie token.
        var service = NewService();

        var authHash = service.Hash(BlogViewerKind.Authenticated, "abc");
        var anonHash = service.Hash(BlogViewerKind.Anonymous, "abc");

        authHash.Should().NotBeEquivalentTo(anonHash,
            "the kind label is folded into the HMAC input to namespace authenticated vs anonymous");
    }

    [Fact]
    public void Hash_OutputIsExactly32Bytes()
    {
        var service = NewService();

        var hash = service.Hash(BlogViewerKind.Anonymous, "tok");

        hash.Should().HaveCount(32,
            "HMAC-SHA256 always produces a 32-byte digest — the BlogView column is binary(32)");
    }

    [Fact]
    public void Hash_WithDifferentSecret_ProducesDifferentOutput()
    {
        // Secret rotation safety: every prior hash becomes invalid after
        // the secret changes, which is the intended behaviour.
        var serviceA = NewService("secret-A");
        var serviceB = NewService("secret-B");

        var a = serviceA.Hash(BlogViewerKind.Anonymous, "x");
        var b = serviceB.Hash(BlogViewerKind.Anonymous, "x");

        a.Should().NotBeEquivalentTo(b);
    }

    [Fact]
    public void Constructor_ThrowsWhenSecretIsMissing()
    {
        // Fail-fast: misconfiguration must not produce silently degraded hashes.
        var options = Options.Create(new ContentBlogsViewerHashOptions
        {
            ViewerHashSecret = string.Empty
        });

        Action act = () => new BlogViewerHashService(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ViewerHashSecret*");
    }

    [Fact]
    public void Hash_ThrowsArgumentException_WhenViewerIdIsEmpty()
    {
        var service = NewService();

        Action act = () => service.Hash(BlogViewerKind.Anonymous, string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    private static BlogViewerHashService NewService(string secret = TestSecret) =>
        new(Options.Create(new ContentBlogsViewerHashOptions
        {
            ViewerHashSecret = secret
        }));
}
