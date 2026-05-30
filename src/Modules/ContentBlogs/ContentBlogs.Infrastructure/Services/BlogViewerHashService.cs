using System.Security.Cryptography;
using System.Text;
using ContentBlogs.Application.Interfaces;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;
using ContentBlogs.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace ContentBlogs.Infrastructure.Services;

public sealed class BlogViewerHashService : IBlogViewerHashService
{
    private readonly byte[] _key;

    public BlogViewerHashService(IOptions<ContentBlogsViewerHashOptions> options)
    {
        var secret = options.Value.ViewerHashSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            // Fail fast at construction time so misconfiguration cannot
            // produce silently-degraded hashes (e.g. all-zero key).
            throw new InvalidOperationException(
                "ContentBlogs:ViewerHashSecret is not configured. " +
                "Set it via user-secrets, environment variables, or another " +
                "secure configuration source before starting the application.");
        }

        _key = Encoding.UTF8.GetBytes(secret);
    }

    public byte[] Hash(BlogViewerKind kind, string viewerId)
    {
        if (string.IsNullOrWhiteSpace(viewerId))
            throw new ArgumentException("ViewerId is required.", nameof(viewerId));

        var label = kind switch
        {
            BlogViewerKind.Authenticated => "user",
            BlogViewerKind.Anonymous     => "anon",
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind), kind, "Unknown BlogViewerKind."),
        };

        var input = Encoding.UTF8.GetBytes($"{label}:{viewerId}");
        return HMACSHA256.HashData(_key, input);
    }
}
