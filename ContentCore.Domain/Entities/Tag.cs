using System.ComponentModel.DataAnnotations;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Tag : BaseEntity
{
    
    private Tag() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Optimistic concurrency token. Backed by a SQL Server rowversion column.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; private set; } = [];

    // ── Factory Method ──
    public static Tag Create(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tag slug is required.", nameof(slug));

        return new Tag
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            IsActive = true
        };
    }

    // ── Business Methods ──
    public void Update(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tag slug is required.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
