using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Domain.Entities;

public sealed class BlogView
{
    public Guid Id { get; private set; }
    public Guid BlogId { get; private set; }
    public byte[] ViewerHash { get; private set; } = [];
    public BlogViewerKind ViewerKind { get; private set; }
    public DateTime ViewedAtUtc { get; private set; }

    private BlogView()
    {
    }

    public static BlogView Create(
        Guid blogId,
        byte[] viewerHash,
        BlogViewerKind viewerKind,
        DateTime utcNow)
    {
        if (blogId == Guid.Empty)
        {
            throw new ArgumentException("BlogId is required.", nameof(blogId));
        }

        if (viewerHash is null || viewerHash.Length != 32)
        {
            throw new ArgumentException(
                "ViewerHash must be a 32-byte HMAC-SHA256 output.", nameof(viewerHash));
        }

        return new BlogView
        {
            Id          = Guid.CreateVersion7(),
            BlogId      = blogId,
            ViewerHash  = viewerHash,
            ViewerKind  = viewerKind,
            ViewedAtUtc = utcNow,
        };
    }
}
