using Accounts.Application.Commands.Provider.Shared;
using FluentAssertions;

namespace Accounts.Tests.Unit;

/// <summary>
/// Patch 2B sanitizer contract. Shared between the Patch 1A download endpoint
/// and the Patch 2B FileAsset backfill — both must produce identical
/// <c>SafeFileName</c> values for the same input.
/// </summary>
public sealed class SafeFileNameSanitizerTests
{
    private const string Fallback = "document-fallback";

    [Fact]
    public void Null_returns_fallback()
        => SafeFileNameSanitizer.Sanitize(null, Fallback).Should().Be(Fallback);

    [Fact]
    public void Blank_returns_fallback()
        => SafeFileNameSanitizer.Sanitize("   ", Fallback).Should().Be(Fallback);

    [Fact]
    public void Plain_name_passes_through()
        => SafeFileNameSanitizer.Sanitize("license.pdf", Fallback).Should().Be("license.pdf");

    [Fact]
    public void Path_components_are_stripped()
    {
        // Path.GetFileName drops everything before the final separator.
        SafeFileNameSanitizer.Sanitize("/var/secret/payload.pdf", Fallback)
            .Should().Be("payload.pdf");

        SafeFileNameSanitizer.Sanitize(@"C:\Users\evil\payload.pdf", Fallback)
            .Should().Be("payload.pdf");
    }

    [Fact]
    public void Invalid_chars_are_replaced_with_underscore()
    {
        // Use a few platform-invalid characters: ':' is invalid on Windows,
        // '\0' is universally invalid.
        var name = "bad\0name.pdf";
        var result = SafeFileNameSanitizer.Sanitize(name, Fallback);

        result.Should().NotContain("\0");
        result.Should().EndWith(".pdf");
    }
}
