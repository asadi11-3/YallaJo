namespace Finance.Domain.Entities;

public sealed class PlanFeature
{
    private PlanFeature() { } // EF Core

    public Guid PlanId { get; private set; }
    public Guid FeatureId { get; private set; }
    public string? Value { get; private set; }

    public SubscriptionPlan SubscriptionPlan { get; private set; } = default!;
    public SubscriptionFeature SubscriptionFeature { get; private set; } = default!;
}
