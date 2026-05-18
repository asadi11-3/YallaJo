using ContentBlogs.Presentation.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace ContentBlogs.Tests.Unit.Presentation;

public sealed class AnonymousViewerProviderTests
{
    private const string CookieName = "yj_av";

    [Fact]
    public void GetOrCreate_GeneratesCookie_WhenAbsent()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("yallajo.com");
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(ctx);

        var provider = new AnonymousViewerProvider(accessor);
        var token = provider.GetOrCreate();

        token.Should().NotBeNullOrWhiteSpace();
        ctx.Response.Headers.SetCookie.ToString().Should()
            .Contain(CookieName,
                "first call must emit a Set-Cookie header on the response");
    }

    [Fact]
    public void GetOrCreate_ReusesCookie_WhenPresent()
    {
        const string existing = "existing-token-value";

        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("yallajo.com");
        ctx.Request.Headers.Cookie = $"{CookieName}={existing}";

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(ctx);

        var provider = new AnonymousViewerProvider(accessor);
        var token = provider.GetOrCreate();

        token.Should().Be(existing,
            "the existing cookie value must be returned verbatim");
        ctx.Response.Headers.SetCookie.ToString().Should().BeEmpty(
            "no new Set-Cookie header should be emitted when the cookie is already present");
    }

    [Fact]
    public void GetOrCreate_GeneratesDistinctTokens_AcrossRequests()
    {
        // Stable randomness: two requests without cookies must receive
        // different tokens.  (Cryptographic random; collision probability
        // is negligible.)
        var token1 = NewRequest_GetOrCreate();
        var token2 = NewRequest_GetOrCreate();

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GetOrCreate_CookieAttributesAreHttpOnlyLaxLongLivedEssential_OnNonLocalhost()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("yallajo.com");
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(ctx);

        new AnonymousViewerProvider(accessor).GetOrCreate();

        var setCookie = ctx.Response.Headers.SetCookie.ToString();
        setCookie.Should().Contain("httponly", "cookie must be HttpOnly");
        setCookie.ToLowerInvariant().Should().Contain("samesite=lax",
            "cookie must use SameSite=Lax for cross-tab tracking compatibility");
        setCookie.Should().Contain("secure",
            "cookie must be Secure outside localhost");
        setCookie.Should().Contain("expires=",
            "cookie must be persistent (Expires set), not session-only");
    }

    [Fact]
    public void GetOrCreate_CookieIsNotMarkedSecure_OnLocalhost()
    {
        // Local-dev convenience: HTTP localhost is allowed without TLS, so
        // the Secure flag is turned off to avoid the cookie being rejected.
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("localhost");
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(ctx);

        new AnonymousViewerProvider(accessor).GetOrCreate();

        var setCookie = ctx.Response.Headers.SetCookie.ToString();
        setCookie.ToLowerInvariant().Should().NotContain("secure",
            "Secure flag must be off on localhost so the cookie isn't rejected over HTTP");
    }

    [Fact]
    public void GetOrCreate_ThrowsInvalidOperation_WhenHttpContextIsNull()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var provider = new AnonymousViewerProvider(accessor);

        Action act = () => provider.GetOrCreate();
        act.Should().Throw<InvalidOperationException>();
    }

    private static string NewRequest_GetOrCreate()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("yallajo.com");
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(ctx);
        return new AnonymousViewerProvider(accessor).GetOrCreate();
    }
}
