using System.Text;
using Accounts.Application.Commands.UpdateAvatar;
using FluentAssertions;

namespace Accounts.Tests.Unit;

/// <summary>
/// Magic-byte / content-type / extension cross-check tests for <see cref="AvatarFileValidator"/>.
/// Only JPEG, PNG and WEBP are accepted; GIF and SVG are rejected; spoofed
/// (signature/content-type/extension mismatch) files are rejected.
/// </summary>
public sealed class AvatarFileValidatorTests
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] WebpBytes =
    [
        0x52, 0x49, 0x46, 0x46, // RIFF
        0x24, 0x00, 0x00, 0x00, // size
        0x57, 0x45, 0x42, 0x50, // WEBP
        0x56, 0x50, 0x38, 0x20, // VP8
    ];
    private static readonly byte[] GifBytes = Encoding.ASCII.GetBytes("GIF89a________");

    private static MemoryStream Stream(byte[] bytes) => new(bytes, writable: false);

    [Theory]
    [InlineData("image/jpeg", "avatar.jpg")]
    [InlineData("image/jpeg", "avatar.jpeg")]
    public void Validate_AcceptsValidJpeg(string contentType, string fileName)
    {
        var result = AvatarFileValidator.Validate(Stream(JpegBytes), contentType, fileName);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsValidPng()
    {
        var result = AvatarFileValidator.Validate(Stream(PngBytes), "image/png", "avatar.png");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsValidWebp()
    {
        var result = AvatarFileValidator.Validate(Stream(WebpBytes), "image/webp", "avatar.webp");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsGarbageBytes()
    {
        var garbage = Encoding.ASCII.GetBytes("not an image at all, just text");

        var result = AvatarFileValidator.Validate(Stream(garbage), "image/png", "avatar.png");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("file");
    }

    [Fact]
    public void Validate_RejectsMismatchedContentType()
    {
        // Real PNG bytes but declared as JPEG.
        var result = AvatarFileValidator.Validate(Stream(PngBytes), "image/jpeg", "avatar.png");

        result.IsValid.Should().BeFalse();
        // .png extension + image/jpeg content type: the content-type allow-list passes,
        // extension allow-list passes, but the signature (PNG) won't match the declared
        // content type (jpeg) -> contentType failure.
        result.Field.Should().Be("contentType");
    }

    [Fact]
    public void Validate_RejectsMismatchedExtension()
    {
        // Real PNG bytes, declared content type image/png, but a .jpg extension.
        var result = AvatarFileValidator.Validate(Stream(PngBytes), "image/png", "avatar.jpg");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("extension");
    }

    [Fact]
    public void Validate_RejectsMismatchedSignature()
    {
        // JPEG declared+extension but PNG bytes -> signature mismatch on content type.
        var result = AvatarFileValidator.Validate(Stream(PngBytes), "image/jpeg", "avatar.jpg");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("contentType");
    }

    [Fact]
    public void Validate_RejectsGif()
    {
        var result = AvatarFileValidator.Validate(Stream(GifBytes), "image/gif", "avatar.gif");

        result.IsValid.Should().BeFalse();
        // GIF content type is not in the allow-list at all.
        result.Field.Should().Be("contentType");
    }

    [Fact]
    public void Validate_RejectsSvg()
    {
        var svg = Encoding.ASCII.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");

        var result = AvatarFileValidator.Validate(Stream(svg), "image/svg+xml", "avatar.svg");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("contentType");
    }

    [Fact]
    public void Validate_RejectsSvgRenamedToPng()
    {
        // SVG bytes but spoofed as PNG (content type + extension): signature detection
        // must reject because the bytes are not a recognised raster image.
        var svg = Encoding.ASCII.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");

        var result = AvatarFileValidator.Validate(Stream(svg), "image/png", "avatar.png");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("file");
    }
}
