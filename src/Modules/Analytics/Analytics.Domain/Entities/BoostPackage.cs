using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class BoostPackage : BaseEntity, IAggregateRoot
{
    private BoostPackage() { } // EF Core

    public Guid ProviderId { get; private set; }
    public EntityType EntityKind { get; private set; }
    public Guid EntityId { get; private set; }
    public decimal BoostMultiplier { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>Billing mode: FlatFee (Bronze/Silver) or CPC (Gold auction).</summary>
    public string BillingMode { get; private set; } = "FlatFee";

    /// <summary>For CPC mode: bid amount per click in base currency.</summary>
    public decimal? BidPerClick { get; private set; }

    /// <summary>For CPC mode: daily budget cap in base currency.</summary>
    public decimal? DailyBudgetCap { get; private set; }

    /// <summary>For CPC mode: amount spent today (reset nightly).</summary>
    public decimal SpentToday { get; private set; }

    public static BoostPackage Create(Guid providerId, EntityType entityKind, Guid entityId, decimal multiplier, DateTime startsAt, DateTime expiresAt)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));

        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));

        if (multiplier <= 0m)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be positive.");

        if (expiresAt <= startsAt)
            throw new ArgumentException("Expiration must be after start.", nameof(expiresAt));

        return new BoostPackage
        {
            ProviderId = providerId,
            EntityKind = entityKind,
            EntityId = entityId,
            BoostMultiplier = multiplier,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public static BoostPackage CreateCpc(Guid providerId, EntityType entityKind, Guid entityId, decimal bidPerClick, decimal dailyBudgetCap, DateTime startsAt, DateTime expiresAt)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("Provider id cannot be empty.", nameof(providerId));
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));
        if (bidPerClick <= 0m)
            throw new ArgumentOutOfRangeException(nameof(bidPerClick), "Bid must be positive.");
        if (dailyBudgetCap <= 0m)
            throw new ArgumentOutOfRangeException(nameof(dailyBudgetCap), "Daily budget must be positive.");
        if (expiresAt <= startsAt)
            throw new ArgumentException("Expiration must be after start.", nameof(expiresAt));

        return new BoostPackage
        {
            ProviderId = providerId,
            EntityKind = entityKind,
            EntityId = entityId,
            BoostMultiplier = 1m, // CPC uses bid, not flat multiplier
            BillingMode = "CPC",
            BidPerClick = bidPerClick,
            DailyBudgetCap = dailyBudgetCap,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            IsActive = true
        };
    }

    public bool HasBudgetRemaining() => BillingMode != "CPC" || SpentToday < (DailyBudgetCap ?? 0m);

    public void ChargeClick(decimal amount)
    {
        if (BillingMode != "CPC") return;
        SpentToday += amount;
    }

    public void ResetDailySpend() => SpentToday = 0m;

    public decimal ComputeDecayedMultiplier(DateTime now)
    {
        if (!IsActive || now >= ExpiresAt || now < StartsAt)
            return 1m;

        var totalDays = (ExpiresAt - StartsAt).TotalDays;
        if (totalDays <= 0)
            return 1m;

        var remainingDays = (ExpiresAt - now).TotalDays;
        var decayFactor = (decimal)(remainingDays / totalDays);
        return 1m + (BoostMultiplier - 1m) * Math.Clamp(decayFactor, 0m, 1m);
    }
}
