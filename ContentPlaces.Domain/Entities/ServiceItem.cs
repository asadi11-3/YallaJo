using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class ServiceItem : AuditableEntity ,IAggregateRoot
{
    private ServiceItem()
    {
    } // EF Core

    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string PriceCurrency { get; private set; } = "JOD";
    public string Currency { get; private set; } = "JOD";
    public ServiceCategory Category { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MaxCapacity { get; private set; }
    public bool IsAvailable { get; private set; }
    public int SortOrder { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public decimal? SalePrice { get; private set; }
    public string? SalePriceCurrency { get; private set; }
    public DateTime? DiscountValidFrom { get; private set; }
    public DateTime? DiscountValidTo { get; private set; }

    public Business Business { get; private set; } = default!;
    public static ServiceItem Create(Guid businessId, string name, decimal price, int durationMinutes, int maxCapacity, string currency, int sortOrder)
    {
        // حماية الدومين (Guard Clauses) حسب شروط الـ PDF
        if (price < 0) throw new ArgumentException("Price cannot be negative", nameof(price));
        if (durationMinutes <= 0) throw new ArgumentException("Duration must be positive", nameof(durationMinutes));
        if (maxCapacity <= 0) throw new ArgumentException("Capacity must be positive", nameof(maxCapacity));

        return new ServiceItem
        {
            Id = Guid.CreateVersion7(),
            BusinessId = businessId,
            Name = name,
            Price = price,
            DurationMinutes = durationMinutes,
            MaxCapacity = maxCapacity,
            Currency = currency.ToUpperInvariant(),
            IsAvailable = true,
            SortOrder = sortOrder,
            IsDeleted = false
        };
    }

    public void Update(string name, decimal price, int durationMinutes, int maxCapacity, string currency, int sortOrder)
    {
        if (price < 0) throw new ArgumentException("Price cannot be negative", nameof(price));
        if (durationMinutes <= 0) throw new ArgumentException("Duration must be positive", nameof(durationMinutes));
        if (maxCapacity <= 0) throw new ArgumentException("Capacity must be positive", nameof(maxCapacity));

        Name = name;
        Price = price;
        DurationMinutes = durationMinutes;
        MaxCapacity = maxCapacity;
        Currency = currency.ToUpperInvariant();
        SortOrder = sortOrder;
    }

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void ApplyDiscount(decimal discountedPrice)
    {
        if (discountedPrice < 0) throw new ArgumentException("Discounted price cannot be negative", nameof(discountedPrice));
        Price = discountedPrice;
    }

    public void RemoveDiscount(decimal originalPrice)
    {
        if (originalPrice < 0) throw new ArgumentException("Original price cannot be negative", nameof(originalPrice));
        Price = originalPrice;
    }
}
