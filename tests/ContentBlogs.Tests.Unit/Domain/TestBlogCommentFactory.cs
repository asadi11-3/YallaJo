using System.Reflection;
using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Tests.Unit.Domain;

/// <summary>
/// Convenience factory for <see cref="BlogComment"/> aggregates and reply chains
/// in unit tests.  Operates purely on the domain — no DB, no DI.
///
/// <para>
/// IMPORTANT: <c>BlogComment.Create</c> validates nesting depth by walking the
/// already-loaded <c>ParentComment</c> navigation chain (see
/// <c>ComputeDepthFromLoadedChain</c>).  In a unit-test context there is no EF
/// Core to populate that navigation; we therefore set it via reflection
/// alongside the FK so the depth invariant can be exercised without spinning
/// up a database.
/// </para>
/// </summary>
internal static class TestBlogCommentFactory
{
    private const string DefaultContent = "This is a thoughtful, valid comment body.";

    /// <summary>
    /// Creates a root-level (no parent) comment on a Published-state blog.
    /// </summary>
    public static BlogComment CreateRoot(
        Guid? blogId = null,
        Guid? userId = null,
        string? content = null,
        DateTime? utcNow = null) =>
        BlogComment.Create(
            blogId:     blogId ?? Guid.NewGuid(),
            userId:     userId ?? Guid.NewGuid(),
            content:    content ?? DefaultContent,
            parent:     null,
            blogStatus: BlogStatus.Published,
            utcNow:     utcNow ?? DateTime.UtcNow);

    /// <summary>
    /// Creates a depth-1 reply to <paramref name="parent"/> and back-fills the
    /// <c>ParentComment</c> navigation so depth validation works in tests.
    /// </summary>
    public static BlogComment CreateReply(
        BlogComment parent,
        Guid? userId = null,
        string? content = null,
        DateTime? utcNow = null)
    {
        var reply = BlogComment.Create(
            blogId:     parent.BlogId,
            userId:     userId ?? Guid.NewGuid(),
            content:    content ?? "Nice point — here's a reply.",
            parent:     parent,
            blogStatus: BlogStatus.Published,
            utcNow:     utcNow ?? DateTime.UtcNow);

        SetParentNavigation(reply, parent);
        return reply;
    }

    /// <summary>
    /// Sets the private <c>BlogComment.ParentComment</c> navigation property
    /// via reflection.  Used to simulate EF Core eager-loading in unit tests
    /// so the domain's depth-check guard can traverse the parent chain.
    /// </summary>
    public static void SetParentNavigation(BlogComment child, BlogComment parent)
    {
        var prop = typeof(BlogComment)
            .GetProperty(
                nameof(BlogComment.ParentComment),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop!.SetValue(child, parent);
    }
}
