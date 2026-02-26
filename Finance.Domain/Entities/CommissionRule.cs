using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class CommissionRule : AuditableEntity
{
    private CommissionRule() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal CommissionPercentage { get; private set; }
    public decimal? MinAmount { get; private set; }
    public decimal? MaxAmount { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public int Priority { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
}
