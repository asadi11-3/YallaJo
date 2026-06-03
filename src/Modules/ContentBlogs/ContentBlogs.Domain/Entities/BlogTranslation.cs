using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities;

public sealed class BlogTranslation : BaseEntity
{
    private BlogTranslation() { } // EF Core

    public Guid BlogId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Summary { get; private set; }

    public Blog Blog { get; private set; } = default!;

    public static BlogTranslation Create(
        Guid blogId,
        Guid languageId,
        string title,
        string content,
        string? summary = null)
    {
        if (blogId == Guid.Empty)
            throw new ArgumentException("Blog is required.", nameof(blogId));
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language is required.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Translation title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Translation content is required.", nameof(content));

        return new BlogTranslation
        {
            BlogId = blogId,
            LanguageId = languageId,
            Title = title.Trim(),
            Content = content.Trim(),
            Summary = summary?.Trim()
        };
    }

    /// <summary>
    /// Updates the localized text of an existing translation. The blog and language
    /// associations are immutable (a translation is uniquely keyed by BlogId + LanguageId).
    /// </summary>
    public void Update(string title, string content, string? summary = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Translation title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Translation content is required.", nameof(content));

        Title = title.Trim();
        Content = content.Trim();
        Summary = summary?.Trim();
    }
}
