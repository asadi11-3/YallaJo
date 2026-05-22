using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class EntityAttributeSnapshot : AuditableEntity, IAggregateRoot
{
    private EntityAttributeSnapshot() { } // EF Core

    public EntityType EntityKind { get; private set; }
    public Guid EntityId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Slug { get; private set; }
    public decimal? BasePriceAmount { get; private set; }
    public string? BasePriceCurrency { get; private set; }
    public decimal? SalePrice { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public int BookingCount { get; private set; }
    public bool IsFeatured { get; private set; }
    public decimal? LocationLatitude { get; private set; }
    public decimal? LocationLongitude { get; private set; }
    public Guid? PlaceId { get; private set; }
    public string? BusinessType { get; private set; }
    public string? Difficulty { get; private set; }
    public int? DurationMinutes { get; private set; }
    public bool IsChildFriendly { get; private set; }
    public bool IsAccessible { get; private set; }
    public bool IsInstantBooking { get; private set; }
    public bool? IsHalal { get; private set; }
    public bool? HasVegetarianOptions { get; private set; }
    public bool? HasAlcoholFreeArea { get; private set; }
    public string? Status { get; private set; }
    public string? CategoryIdsJson { get; private set; }
    public int? UpcomingCapacity { get; private set; }
    public int? UpcomingBookings { get; private set; }
    public bool IsPhotogenicHotspot { get; private set; }
    public DateTime LastSnapshotAt { get; private set; }

    public static EntityAttributeSnapshot CreateForTour(
        Guid entityId,
        string name,
        string? slug,
        decimal? basePriceAmount,
        string? basePriceCurrency,
        decimal? salePrice,
        decimal averageRating,
        int reviewCount,
        int bookingCount,
        bool isFeatured,
        decimal? locationLatitude,
        decimal? locationLongitude,
        Guid? placeId,
        string? difficulty,
        int? durationMinutes,
        bool isChildFriendly,
        bool isAccessible,
        bool isInstantBooking,
        string? status,
        string? categoryIdsJson,
        DateTime lastSnapshotAt)
        => Create(
            EntityType.Tour,
            entityId,
            name,
            slug,
            basePriceAmount,
            basePriceCurrency,
            salePrice,
            averageRating,
            reviewCount,
            bookingCount,
            isFeatured,
            locationLatitude,
            locationLongitude,
            placeId,
            businessType: null,
            difficulty,
            durationMinutes,
            isChildFriendly,
            isAccessible,
            isInstantBooking,
            isHalal: null,
            hasVegetarianOptions: null,
            hasAlcoholFreeArea: null,
            status,
            categoryIdsJson,
            lastSnapshotAt);

    public static EntityAttributeSnapshot CreateForBusiness(
        Guid entityId,
        string name,
        string? slug,
        decimal? basePriceAmount,
        string? basePriceCurrency,
        decimal? salePrice,
        decimal averageRating,
        int reviewCount,
        int bookingCount,
        bool isFeatured,
        decimal? locationLatitude,
        decimal? locationLongitude,
        Guid? placeId,
        string? businessType,
        bool isChildFriendly,
        bool isAccessible,
        bool isInstantBooking,
        bool? isHalal,
        bool? hasVegetarianOptions,
        bool? hasAlcoholFreeArea,
        string? status,
        string? categoryIdsJson,
        DateTime lastSnapshotAt)
        => Create(
            EntityType.Business,
            entityId,
            name,
            slug,
            basePriceAmount,
            basePriceCurrency,
            salePrice,
            averageRating,
            reviewCount,
            bookingCount,
            isFeatured,
            locationLatitude,
            locationLongitude,
            placeId,
            businessType,
            difficulty: null,
            durationMinutes: null,
            isChildFriendly,
            isAccessible,
            isInstantBooking,
            isHalal,
            hasVegetarianOptions,
            hasAlcoholFreeArea,
            status,
            categoryIdsJson,
            lastSnapshotAt);

    public static EntityAttributeSnapshot CreateForPlace(
        Guid entityId,
        string name,
        string? slug,
        decimal averageRating,
        int reviewCount,
        int bookingCount,
        bool isFeatured,
        decimal? locationLatitude,
        decimal? locationLongitude,
        bool isChildFriendly,
        bool isAccessible,
        string? status,
        string? categoryIdsJson,
        DateTime lastSnapshotAt)
        => Create(
            EntityType.Place,
            entityId,
            name,
            slug,
            basePriceAmount: null,
            basePriceCurrency: null,
            salePrice: null,
            averageRating,
            reviewCount,
            bookingCount,
            isFeatured,
            locationLatitude,
            locationLongitude,
            placeId: null,
            businessType: null,
            difficulty: null,
            durationMinutes: null,
            isChildFriendly,
            isAccessible,
            isInstantBooking: false,
            isHalal: null,
            hasVegetarianOptions: null,
            hasAlcoholFreeArea: null,
            status,
            categoryIdsJson,
            lastSnapshotAt);

    public void UpdateFrom(
        string name,
        string? slug,
        decimal? basePriceAmount,
        string? basePriceCurrency,
        decimal? salePrice,
        decimal averageRating,
        int reviewCount,
        int bookingCount,
        bool isFeatured,
        decimal? locationLatitude,
        decimal? locationLongitude,
        Guid? placeId,
        string? businessType,
        string? difficulty,
        int? durationMinutes,
        bool isChildFriendly,
        bool isAccessible,
        bool isInstantBooking,
        bool? isHalal,
        bool? hasVegetarianOptions,
        bool? hasAlcoholFreeArea,
        string? status,
        string? categoryIdsJson,
        DateTime lastSnapshotAt)
    {
        SetSnapshot(
            name,
            slug,
            basePriceAmount,
            basePriceCurrency,
            salePrice,
            averageRating,
            reviewCount,
            bookingCount,
            isFeatured,
            locationLatitude,
            locationLongitude,
            placeId,
            businessType,
            difficulty,
            durationMinutes,
            isChildFriendly,
            isAccessible,
            isInstantBooking,
            isHalal,
            hasVegetarianOptions,
            hasAlcoholFreeArea,
            status,
            categoryIdsJson,
            lastSnapshotAt);

        MarkUpdated();
    }

    public void UpdateInventory(int? upcomingCapacity, int? upcomingBookings)
    {
        UpcomingCapacity = upcomingCapacity;
        UpcomingBookings = upcomingBookings;
        MarkUpdated();
    }

    public void SetPhotogenic(bool isPhotogenicHotspot)
    {
        IsPhotogenicHotspot = isPhotogenicHotspot;
        MarkUpdated();
    }

    private static EntityAttributeSnapshot Create(
        EntityType entityKind,
        Guid entityId,
        string name,
        string? slug,
        decimal? basePriceAmount,
        string? basePriceCurrency,
        decimal? salePrice,
        decimal averageRating,
        int reviewCount,
        int bookingCount,
        bool isFeatured,
        decimal? locationLatitude,
        decimal? locationLongitude,
        Guid? placeId,
        string? businessType,
        string? difficulty,
        int? durationMinutes,
        bool isChildFriendly,
        bool isAccessible,
        bool isInstantBooking,
        bool? isHalal,
        bool? hasVegetarianOptions,
        bool? hasAlcoholFreeArea,
        string? status,
        string? categoryIdsJson,
        DateTime lastSnapshotAt)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));

        var snapshot = new EntityAttributeSnapshot
        {
            EntityKind = entityKind,
            EntityId = entityId
        };

        snapshot.SetSnapshot(
            name,
            slug,
            basePriceAmount,
            basePriceCurrency,
            salePrice,
            averageRating,
            reviewCount,
            bookingCount,
            isFeatured,
            locationLatitude,
            locationLongitude,
            placeId,
            businessType,
            difficulty,
            durationMinutes,
            isChildFriendly,
            isAccessible,
            isInstantBooking,
            isHalal,
            hasVegetarianOptions,
            hasAlcoholFreeArea,
            status,
            categoryIdsJson,
            lastSnapshotAt);

        return snapshot;
    }

    private void SetSnapshot(
        string name,
        string? slug,
        decimal? basePriceAmount,
        string? basePriceCurrency,
        decimal? salePrice,
        decimal averageRating,
        int reviewCount,
        int bookingCount,
        bool isFeatured,
        decimal? locationLatitude,
        decimal? locationLongitude,
        Guid? placeId,
        string? businessType,
        string? difficulty,
        int? durationMinutes,
        bool isChildFriendly,
        bool isAccessible,
        bool isInstantBooking,
        bool? isHalal,
        bool? hasVegetarianOptions,
        bool? hasAlcoholFreeArea,
        string? status,
        string? categoryIdsJson,
        DateTime lastSnapshotAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (reviewCount < 0)
            throw new ArgumentOutOfRangeException(nameof(reviewCount), "Review count cannot be negative.");

        if (bookingCount < 0)
            throw new ArgumentOutOfRangeException(nameof(bookingCount), "Booking count cannot be negative.");

        Name = name.Trim();
        Slug = string.IsNullOrWhiteSpace(slug) ? null : slug.Trim();
        BasePriceAmount = basePriceAmount;
        BasePriceCurrency = string.IsNullOrWhiteSpace(basePriceCurrency) ? null : basePriceCurrency.Trim().ToUpperInvariant();
        SalePrice = salePrice;
        AverageRating = averageRating;
        ReviewCount = reviewCount;
        BookingCount = bookingCount;
        IsFeatured = isFeatured;
        LocationLatitude = locationLatitude;
        LocationLongitude = locationLongitude;
        PlaceId = placeId;
        BusinessType = string.IsNullOrWhiteSpace(businessType) ? null : businessType.Trim();
        Difficulty = string.IsNullOrWhiteSpace(difficulty) ? null : difficulty.Trim();
        DurationMinutes = durationMinutes;
        IsChildFriendly = isChildFriendly;
        IsAccessible = isAccessible;
        IsInstantBooking = isInstantBooking;
        IsHalal = isHalal;
        HasVegetarianOptions = hasVegetarianOptions;
        HasAlcoholFreeArea = hasAlcoholFreeArea;
        Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        CategoryIdsJson = string.IsNullOrWhiteSpace(categoryIdsJson) ? null : categoryIdsJson.Trim();
        LastSnapshotAt = lastSnapshotAt;
    }
}
