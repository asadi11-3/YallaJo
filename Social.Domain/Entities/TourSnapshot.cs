using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>Social read-model snapshot of a ContentTours tour.</summary>
public sealed class TourSnapshot : BaseEntity
{
    private TourSnapshot() { } // EF Core

    private TourSnapshot(Guid tourId, string name, string slug, Guid ownerProviderId, Guid? placeId, DateTime nowUtc)
    {
        TourId = tourId;
        Name = name;
        Slug = slug;
        OwnerProviderId = ownerProviderId;
        PlaceId = placeId;
        CreatedAt = nowUtc;
    }

    public Guid TourId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? BusinessId { get; private set; }
    public Guid OwnerProviderId { get; private set; }
    public Guid? PlaceId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public static TourSnapshot Create(Guid tourId, string name, string slug, Guid ownerProviderId, Guid? placeId, DateTime nowUtc)
        => new(tourId, name, slug, ownerProviderId, placeId, nowUtc);

    public void UpsertCreated(string name, string slug, Guid ownerProviderId, Guid? placeId, DateTime nowUtc)
    {
        Name = name;
        Slug = slug;
        OwnerProviderId = ownerProviderId;
        PlaceId = placeId;
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = nowUtc;
    }

    public void MarkUpdated(DateTime nowUtc)
    {
        UpdatedAt = nowUtc;
    }

    public void MarkDeleted(Guid ownerProviderId, Guid? placeId, DateTime deletedAt)
    {
        OwnerProviderId = ownerProviderId;
        PlaceId = placeId;
        IsDeleted = true;
        DeletedAt = deletedAt;
        UpdatedAt = deletedAt;
    }
}
