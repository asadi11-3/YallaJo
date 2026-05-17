using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

public sealed class CommissionRule : AuditableEntity, IAggregateRoot
{
    private CommissionRule() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal CommissionPercentage { get; private set; }
    public Money MinAmount { get; private set; } = default!;
    public Money MaxAmount { get; private set; } = default!;
    public string EntityType { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public int Priority { get; private set; }
    public DateRange? ValidityPeriod { get; private set; }
}
