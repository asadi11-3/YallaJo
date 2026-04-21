using FluentAssertions;
using Microsoft.Extensions.Configuration;
using YallaJo.Web.Services;

namespace Web.Tests.Unit;

/// <summary>
/// The avatar rendered by the web app was broken because API-hosted files
/// ("/uploads/avatars/<guid>.png") were interpreted by the browser as being on
/// the WEB origin. The resolver prepends the configured ApiBaseUrl so &lt;img src&gt;
/// hits the API host instead.
/// </summary>
public sealed class ApiAssetUrlResolverTests
{
    private static ApiAssetUrlResolver Build(string? apiBaseUrl)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = apiBaseUrl,
            })
            .Build();
        return new ApiAssetUrlResolver(cfg);
    }

    [Fact]
    public void Resolve_ShouldPrependApiBaseUrl_ForRelativeApiPath()
    {
        var sut = Build("https://localhost:57065");

        var absolute = sut.Resolve("/uploads/avatars/abc.png");

        absolute.Should().Be("https://localhost:57065/uploads/avatars/abc.png");
    }

    [Fact]
    public void Resolve_ShouldStripTrailingSlash_OnBaseUrl()
    {
        var sut = Build("https://localhost:57065/");

        sut.Resolve("/uploads/avatars/abc.png")
            .Should().Be("https://localhost:57065/uploads/avatars/abc.png");
    }

    [Fact]
    public void Resolve_ShouldPrefixLeadingSlash_WhenPathHasNone()
    {
        var sut = Build("https://localhost:57065");

        sut.Resolve("uploads/avatars/abc.png")
            .Should().Be("https://localhost:57065/uploads/avatars/abc.png");
    }

    [Theory]
    [InlineData("https://cdn.example.com/avatars/abc.png")]
    [InlineData("http://cdn.example.com/avatars/abc.png")]
    public void Resolve_ShouldLeaveAbsoluteUrls_Untouched(string absolute)
    {
        var sut = Build("https://localhost:57065");

        sut.Resolve(absolute).Should().Be(absolute);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_ShouldReturnInput_WhenNullOrWhitespace(string? input)
    {
        var sut = Build("https://localhost:57065");

        sut.Resolve(input).Should().Be(input);
    }

    [Fact]
    public void Resolve_ShouldPassThroughRelativePath_WhenNoBaseUrlConfigured()
    {
        var sut = Build(null);

        sut.Resolve("/uploads/avatars/abc.png")
            .Should().Be("/uploads/avatars/abc.png");
    }
}
