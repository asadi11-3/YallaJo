using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>Social read-model snapshot of a ContentPlaces place.</summary>
public sealed class PlaceSnapshot : BaseEntity
{
    private PlaceSnapshot() { } // EF Core

    private PlaceSnapshot(Guid placeId, string name, string slug, DateTime nowUtc)
    {
        PlaceId = placeId;
        Name = name;
        Slug = slug;
        CreatedAt = nowUtc;
    }

    public Guid PlaceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Address { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static PlaceSnapshot Create(Guid placeId, string name, string slug, DateTime nowUtc)
        => new(placeId, name, slug, nowUtc);

    public void UpsertCreated(string name, string slug, DateTime nowUtc)
    {
        Name = name;
        Slug = slug;
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = nowUtc;
    }

    public void Update(string name, string? description, string? address, DateTime nowUtc)
    {
        Name = name;
        Description = description;
        Address = address;
        UpdatedAt = nowUtc;
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
    }
}
