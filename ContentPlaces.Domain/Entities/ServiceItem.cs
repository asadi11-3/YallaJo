using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentPlaces.Domain.Entities;

public sealed class ServiceItem : AuditableEntity
{
    private ServiceItem() { } // EF Core

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
}
