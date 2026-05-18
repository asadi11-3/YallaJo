using System.Reflection;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using FluentAssertions;

namespace ContentBlogs.Tests.Unit.Domain;

public sealed class BlogViewPrivacyTests
{
    [Fact]
    public void BlogView_DoesNotExpose_RawIdentityFields()
    {
        var forbiddenNames = new[]
        {
            "UserId",
            "CookieToken",
            "IpAddress",
            "UserAgent",
            "RawViewerId",
            "ViewerId",
        };

        var props = typeof(BlogView)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToList();

        foreach (var name in forbiddenNames)
        {
            props.Should().NotContain(name,
                $"BlogView must not expose '{name}' — privacy contract " +
                "(CONTENTBLOGS-VIEW-DEBOUNCE-IMPL-001).  Use HMAC-SHA256 ViewerHash instead.");
        }
    }

    [Fact]
    public void BlogView_ExposesViewerHash_AsBinary32()
    {
        var prop = typeof(BlogView).GetProperty(
            nameof(BlogView.ViewerHash),
            BindingFlags.Instance | BindingFlags.Public);

        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be(typeof(byte[]));
    }

    [Fact]
    public void BlogView_Create_RejectsViewerHashShorterThan32Bytes()
    {
        // Defensive guard so callers can't accidentally pass a truncated or
        // non-HMAC byte buffer.
        Action act = () => BlogView.Create(
            blogId:     Guid.NewGuid(),
            viewerHash: new byte[16],   // wrong length
            viewerKind: BlogViewerKind.Anonymous,
            utcNow:     DateTime.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*32-byte HMAC-SHA256*");
    }

    [Fact]
    public void BlogView_Create_RejectsEmptyBlogId()
    {
        Action act = () => BlogView.Create(
            blogId:     Guid.Empty,
            viewerHash: new byte[32],
            viewerKind: BlogViewerKind.Anonymous,
            utcNow:     DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }
}
