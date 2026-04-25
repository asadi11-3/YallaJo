using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

/// <summary>
/// A service offered by a <see cref="Business"/> (e.g., guided tour, massage, cooking class).
/// Non-aggregate entity — does NOT implement <see cref="IAggregateRoot"/>.
/// Integration events are published from command handlers via IContentPlacesOutboxWriter.
/// </summary>
public sealed class ServiceItem : AuditableEntity
{
    private ServiceItem() { } // EF Core

    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "JOD";
    public ServiceCategory Category { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsAvailable { get; private set; }
    public int SortOrder { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public decimal? SalePrice { get; private set; }
    public DateTime? DiscountValidFrom { get; private set; }
    public DateTime? DiscountValidTo { get; private set; }

    public Business Business { get; private set; } = default!;

    public static ServiceItem Create(
        Guid businessId,
        string name,
        decimal price,
        string currency,
        ServiceCategory category,
        int durationMinutes,
        int maxCapacity,
        string? description = null,
        int sortOrder = 0)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be positive.", nameof(durationMinutes));
        if (maxCapacity <= 0)
            throw new ArgumentException("Capacity must be positive.", nameof(maxCapacity));

        return new ServiceItem
        {
            Id              = Guid.CreateVersion7(),
            BusinessId      = businessId,
            Name            = name.Trim(),
            Description     = description?.Trim(),
            Price           = price,
            Currency        = currency.ToUpperInvariant(),
            Category        = category,
            DurationMinutes = durationMinutes,
            MaxCapacity     = maxCapacity,
            IsAvailable     = true,
            SortOrder       = sortOrder,
        };
    }

    public void Update(
        string name,
        decimal price,
        string currency,
        ServiceCategory category,
        int durationMinutes,
        int maxCapacity,
        string? description = null,
        int sortOrder = 0)
    {
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));
        if (durationMinutes <= 0)
            throw new ArgumentException("Duration must be positive.", nameof(durationMinutes));
        if (maxCapacity <= 0)
            throw new ArgumentException("Capacity must be positive.", nameof(maxCapacity));

        Name            = name.Trim();
        Description     = description?.Trim();
        Price           = price;
        Currency        = currency.ToUpperInvariant();
        Category        = category;
        DurationMinutes = durationMinutes;
        MaxCapacity     = maxCapacity;
        SortOrder       = sortOrder;
        MarkUpdated();
    }

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        MarkUpdated();
    }
}
