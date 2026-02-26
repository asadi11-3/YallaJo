using Finance.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class Subscription : AuditableEntity
{
    private Subscription() { } // EF Core

    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.Active;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }

    public SubscriptionPlan SubscriptionPlan { get; private set; } = default!;
}
