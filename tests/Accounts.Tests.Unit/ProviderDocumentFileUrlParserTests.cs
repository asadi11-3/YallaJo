using Accounts.Application.Commands.Provider.Shared;
using FluentAssertions;

namespace Accounts.Tests.Unit;

/// <summary>
/// Patch 2B parser contract. Inputs are real-shape legacy production URLs
/// e.g. <c>/uploads/provider-application-documents/{guidv7}{ext}</c>.
/// Returned <c>StorageKey</c> values are folder-relative and never include
/// the base URL — they are the SAME keys the upload handler writes.
/// </summary>
public sealed class ProviderDocumentFileUrlParserTests
{
    private const string BaseUrl = "/uploads";

    [Theory]
    [InlineData("/uploads/provider-application-documents/01910000-0000-7000-8000-000000000001.pdf", "provider-application-documents/01910000-0000-7000-8000-000000000001.pdf", ".pdf", "application/pdf")]
    [InlineData("/uploads/provider-application-documents/abc.JPG",                              "provider-application-documents/abc.JPG",                              ".jpg", "image/jpeg")]
    [InlineData("/uploads/provider-application-documents/abc.jpeg",                             "provider-application-documents/abc.jpeg",                             ".jpeg", "image/jpeg")]
    [InlineData("/uploads/provider-application-documents/abc.png",                              "provider-application-documents/abc.png",                              ".png", "image/png")]
    public void Parses_supported_extension(string url, string storageKey, string ext, string ct)
    {
        var result = ProviderDocumentFileUrlParser.Parse(url, BaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.StorageKey.Should().Be(storageKey);
        result.Extension.Should().Be(ext);
        result.ContentType.Should().Be(ct);
        result.SkipReason.Should().BeNull();
    }

    [Fact]
    public void Null_or_blank_FileUrl_returns_NoFileUrl()
    {
        ProviderDocumentFileUrlParser.Parse(null, BaseUrl).SkipReason.Should().Be("NoFileUrl");
        ProviderDocumentFileUrlParser.Parse("", BaseUrl).SkipReason.Should().Be("NoFileUrl");
        ProviderDocumentFileUrlParser.Parse("   ", BaseUrl).SkipReason.Should().Be("NoFileUrl");
    }

    [Theory]
    [InlineData("/uploads/tours/abc.pdf")]                   // wrong folder
    [InlineData("/static/provider-application-documents/abc.pdf")] // wrong base
    [InlineData("provider-application-documents/abc.pdf")]   // missing leading slash
    public void Wrong_prefix_returns_InvalidPrefix(string url)
    {
        var result = ProviderDocumentFileUrlParser.Parse(url, BaseUrl);
        result.IsSuccess.Should().BeFalse();
        result.SkipReason.Should().Be("InvalidPrefix");
    }

    [Theory]
    [InlineData("/uploads/provider-application-documents/")]                            // empty tail
    [InlineData("/uploads/provider-application-documents/../etc/passwd")]               // traversal
    [InlineData("/uploads/provider-application-documents/sub/abc.pdf")]                 // multi-segment
    [InlineData("/uploads/provider-application-documents/evil\\abc.pdf")]               // backslash
    public void Traversal_or_multi_segment_returns_InvalidPath(string url)
    {
        var result = ProviderDocumentFileUrlParser.Parse(url, BaseUrl);
        result.IsSuccess.Should().BeFalse();
        result.SkipReason.Should().Be("InvalidPath");
    }

    [Theory]
    [InlineData("/uploads/provider-application-documents/abc.exe")]
    [InlineData("/uploads/provider-application-documents/abc.gif")]
    [InlineData("/uploads/provider-application-documents/abc.svg")]
    [InlineData("/uploads/provider-application-documents/abc")]
    public void Unsupported_extension_returns_InvalidExtension(string url)
    {
        var result = ProviderDocumentFileUrlParser.Parse(url, BaseUrl);
        result.IsSuccess.Should().BeFalse();
        result.SkipReason.Should().Be("InvalidExtension");
    }

    [Fact]
    public void Case_insensitive_prefix_match()
    {
        var result = ProviderDocumentFileUrlParser.Parse(
            "/UPLOADS/PROVIDER-APPLICATION-DOCUMENTS/abc.pdf", BaseUrl);

        result.IsSuccess.Should().BeTrue();
        // StorageKey case is preserved AS-IS from the tail (we don't lower-case
        // the segment because it's an opaque storage key; only the extension is normalized).
        result.StorageKey.Should().EndWith("abc.pdf");
        result.Extension.Should().Be(".pdf");
    }

    [Fact]
    public void Trailing_slash_baseUrl_is_normalized()
    {
        var result = ProviderDocumentFileUrlParser.Parse(
            "/uploads/provider-application-documents/abc.pdf",
            "/uploads/");

        result.IsSuccess.Should().BeTrue();
        result.StorageKey.Should().Be("provider-application-documents/abc.pdf");
    }
}
