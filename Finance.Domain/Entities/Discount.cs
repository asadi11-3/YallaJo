using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class Discount : AuditableEntity
{
    private readonly List<DiscountUsage> _discountUsages = [];

    private Discount() { } // EF Core

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal? MinOrderAmount { get; private set; }
    public decimal? MaxDiscountAmount { get; private set; }
    public int? MaxUsageCount { get; private set; }
    public int CurrentUsageCount { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }

    public IReadOnlyCollection<DiscountUsage> DiscountUsages => _discountUsages.AsReadOnly();
}
