using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class SubscriptionFeature : AuditableEntity
{
    private SubscriptionFeature() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Code { get; private set; } = string.Empty;
}
