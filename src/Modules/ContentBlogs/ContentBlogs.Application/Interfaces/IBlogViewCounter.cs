using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Application.Interfaces;

public interface IBlogViewCounter
{
    Task<BlogViewCountResult> TryCountAsync(
        Guid blogId,
        BlogViewerKind viewerKind,
        byte[] viewerHash,
        CancellationToken cancellationToken = default);
}

public sealed record BlogViewCountResult(
    bool Counted,
    int? ViewCount,
    string? Slug);
