using ContentBlogs.Domain.Entities;
using ContentBlogs.Domain.Enums;

namespace ContentBlogs.Tests.Unit.Domain;

/// <summary>
/// Convenience factory that creates <see cref="Blog"/> instances in various
/// lifecycle states without requiring infrastructure dependencies.
/// </summary>
internal static class TestBlogFactory
{
    private static readonly Guid DefaultAuthor = Guid.NewGuid();
    private static readonly Guid DefaultLanguage = Guid.NewGuid();

    // ── Base factory ────────────────────────────────────────────────────────

    public static Blog CreateDraft(
        string? title = null,
        string? slug = null,
        Guid? authorId = null,
        Guid? sourceLanguageId = null,
        Guid? placeId = null,
        DateTime? utcNow = null)
    {
        return Blog.Create(
            title:            title ?? "Petra Sunrise: A Practical Guide for First-Time Visitors",
            slug:             slug ?? $"petra-sunrise-{Guid.NewGuid():N}",
            content:          new string('x', 200), // satisfies any min-length check
            authorId:         authorId ?? DefaultAuthor,
            sourceLanguageId: sourceLanguageId ?? DefaultLanguage,
            utcNow:           utcNow ?? DateTime.UtcNow);
    }

    // ── State helpers ───────────────────────────────────────────────────────

    public static Blog CreatePublished(
        Guid? authorId = null,
        Guid? placeId = null,
        DateTime? publishedAt = null)
    {
        var blog = CreateDraft(authorId: authorId, placeId: placeId);
        blog.ClearDomainEvents();
        blog.Publish(publishedAt ?? DateTime.UtcNow);
        return blog;
    }

    public static Blog CreateArchived(DateTime? archivedAt = null)
    {
        var blog = CreatePublished();
        blog.ClearDomainEvents();
        blog.Archive(archivedAt ?? DateTime.UtcNow);
        return blog;
    }

    public static Blog CreateDeleted(DateTime? deletedAt = null)
    {
        var blog = CreateDraft();
        blog.ClearDomainEvents();
        blog.Delete(deletedAt ?? DateTime.UtcNow);
        return blog;
    }
}
