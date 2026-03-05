using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace Finance.Domain.Entities;

public sealed class SubscriptionPlan : AuditableEntity
{
    private readonly List<PlanFeature> _planFeatures = [];
    private readonly List<Subscription> _subscriptions = [];

    private SubscriptionPlan() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Money Price { get; private set; } = default!;
    public string Currency { get; private set; } = string.Empty;
    public BillingCycle BillingCycle { get; private set; }
    public int TrialDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    public IReadOnlyCollection<PlanFeature> PlanFeatures => _planFeatures.AsReadOnly();
    public IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();
}
