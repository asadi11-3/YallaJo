using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities;

public sealed class Blog : AuditableEntity, IAggregateRoot
{
    private readonly List<BlogTranslation> _blogTranslations = [];
    private readonly List<BlogComment> _blogComments = [];

    private Blog() { } // EF Core

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public Guid AuthorId { get; private set; }
    public BlogStatus Status { get; private set; } = BlogStatus.Draft;
    public bool IsFeatured { get; private set; } = false;
    public int ViewCount { get; private set; } = 0;
    public int? ReadTimeMinutes { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    public IReadOnlyCollection<BlogTranslation> BlogTranslations => _blogTranslations.AsReadOnly();
    public IReadOnlyCollection<BlogComment> BlogComments => _blogComments.AsReadOnly();
}
