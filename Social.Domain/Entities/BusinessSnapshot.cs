using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>Social read-model snapshot of a ContentPlaces business.</summary>
public sealed class BusinessSnapshot : BaseEntity
{
    private BusinessSnapshot() { } // EF Core

    private BusinessSnapshot(Guid businessId, string name, string slug, Guid ownerId, Guid? placeId, DateTime nowUtc)
    {
        BusinessId = businessId;
        Name = name;
        Slug = slug;
        OwnerId = ownerId;
        PlaceId = placeId;
        CreatedAt = nowUtc;
    }

    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid OwnerId { get; private set; }
    public Guid? PlaceId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static BusinessSnapshot Create(Guid businessId, string name, string slug, Guid ownerId, Guid? placeId, DateTime nowUtc)
        => new(businessId, name, slug, ownerId, placeId, nowUtc);

    public void UpsertCreated(string name, string slug, Guid ownerId, Guid? placeId, DateTime nowUtc)
    {
        Name = name;
        Slug = slug;
        OwnerId = ownerId;
        PlaceId = placeId;
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = nowUtc;
    }

    public void Update(string name, string slug, Guid ownerId, Guid? placeId, DateTime nowUtc)
    {
        UpsertCreated(name, slug, ownerId, placeId, nowUtc);
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
    }
}
