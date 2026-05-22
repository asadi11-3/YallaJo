using YallaJo.SharedKernel.Domain.Entities;

namespace ContentBlogs.Domain.Entities.Creators;

/// <summary>
/// Admin-curated taxonomy of creator niches (e.g. Adventure, Cultural, Food).
/// Wave 7 – Content Creator Module.
/// </summary>
public sealed class CreatorNiche : AuditableEntity
{
    /// <summary>Display name of the niche (e.g. "Adventure Tourism").</summary>
    public string Name { get; private set; } = null!;

    /// <summary>URL-friendly slug derived from the name.</summary>
    public string Slug { get; private set; } = null!;

    /// <summary>Optional description shown to applicants.</summary>
    public string? Description { get; private set; }

    /// <summary>Sort order for display purposes.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Whether this niche is currently selectable by applicants.</summary>
    public bool IsActive { get; private set; } = true;

    // EF constructor
    private CreatorNiche() { }

    /// <summary>Creates a new niche (admin action).</summary>
    public static CreatorNiche Create(string name, string slug, string? description, int sortOrder)
    {
        return new CreatorNiche
        {
            Name = name,
            Slug = slug,
            Description = description,
            SortOrder = sortOrder,
            IsActive = true
        };
    }

    public void Update(string name, string slug, string? description, int sortOrder)
    {
        Name = name;
        Slug = slug;
        Description = description;
        SortOrder = sortOrder;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }
}
