using System.Text;
using ContentTours.Application.Commands.TourGuides.UploadAvatar;
using FluentAssertions;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Validation tests for <see cref="TourGuideAvatarFileValidator"/> (TG-AVATAR-B).
/// The managed tour-guide avatar upload only accepts real JPEG/PNG/WEBP images
/// whose magic bytes match BOTH the declared content type and the file extension.
/// </summary>
public sealed class TourGuideAvatarFileValidatorTests
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] WebpBytes =
    [
        0x52, 0x49, 0x46, 0x46, // RIFF
        0x24, 0x00, 0x00, 0x00, // file size
        0x57, 0x45, 0x42, 0x50, // WEBP
        0x56, 0x50, 0x38, 0x20  // VP8 chunk
    ];

    private static MemoryStream Stream(byte[] bytes) => new(bytes, writable: false);

    [Theory]
    [InlineData("image/jpeg", "avatar.jpg")]
    [InlineData("image/jpeg", "avatar.jpeg")]
    public void Validate_AcceptsValidJpeg(string contentType, string fileName)
    {
        var result = TourGuideAvatarFileValidator.Validate(Stream(JpegBytes), contentType, fileName);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsValidPng()
    {
        var result = TourGuideAvatarFileValidator.Validate(Stream(PngBytes), "image/png", "avatar.png");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AcceptsValidWebp()
    {
        var result = TourGuideAvatarFileValidator.Validate(Stream(WebpBytes), "image/webp", "avatar.webp");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsSvg()
    {
        var svg = Encoding.ASCII.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>");

        var result = TourGuideAvatarFileValidator.Validate(Stream(svg), "image/svg+xml", "avatar.svg");

        result.IsValid.Should().BeFalse();
        // SVG content type is not in the allow-list, so it fails on content type first.
        result.Field.Should().Be("contentType");
    }

    [Fact]
    public void Validate_RejectsGif()
    {
        var gif = Encoding.ASCII.GetBytes("GIF89a");

        var result = TourGuideAvatarFileValidator.Validate(Stream(gif), "image/gif", "avatar.gif");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("contentType");
    }

    [Fact]
    public void Validate_RejectsGarbageBytesWithAllowedContentTypeAndExtension()
    {
        var garbage = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };

        var result = TourGuideAvatarFileValidator.Validate(Stream(garbage), "image/png", "avatar.png");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("file");
    }

    [Fact]
    public void Validate_RejectsContentTypeSignatureMismatch()
    {
        // PNG bytes but declared as JPEG (extension also jpg to pass the extension allow-list).
        var result = TourGuideAvatarFileValidator.Validate(Stream(PngBytes), "image/jpeg", "avatar.jpg");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("contentType");
    }

    [Fact]
    public void Validate_RejectsExtensionSignatureMismatch()
    {
        // PNG bytes, declared image/png, but file extension is .jpg.
        var result = TourGuideAvatarFileValidator.Validate(Stream(PngBytes), "image/png", "avatar.jpg");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("extension");
    }

    [Fact]
    public void Validate_RejectsUnsupportedExtension()
    {
        var result = TourGuideAvatarFileValidator.Validate(Stream(JpegBytes), "image/jpeg", "avatar.gif");

        result.IsValid.Should().BeFalse();
        result.Field.Should().Be("extension");
    }
}
