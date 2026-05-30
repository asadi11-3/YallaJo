using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

public sealed class Referral : AuditableEntity, IAggregateRoot
{
    private Referral() { } // EF Core

    public Guid ReferrerUserId { get; private set; }
    public Guid? ReferredUserId { get; private set; }
    public string ReferralCode { get; private set; } = string.Empty;
    public byte Status { get; private set; }
    public decimal? RewardAmount { get; private set; }
    public decimal? ReferredRewardAmount { get; private set; }
    public string? Currency { get; private set; }
    public DateTime? CompletedAt { get; private set; }
}
