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
}
