using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

public sealed class CommissionSnapshot : BaseEntity, IAggregateRoot
{
    private CommissionSnapshot() { } // EF Core

    public string Tier { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal Percentage { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime LastUpdatedAt { get; private set; }

    public static CommissionSnapshot Create(
        Guid ruleId,
        string tier,
        string currency,
        decimal percentage,
        DateTime occurredAt)
    {
        Validate(ruleId, tier, currency, percentage);

        var snapshot = new CommissionSnapshot
        {
            Tier = tier.Trim(),
            Currency = currency.ToUpperInvariant(),
            Percentage = percentage,
            IsActive = true,
            LastUpdatedAt = occurredAt,
        };
        snapshot.Id = ruleId; 
        return snapshot;
    }

    public void ApplyUpsert(string tier, string currency, decimal percentage, DateTime occurredAt)
    {
        Validate(Id, tier, currency, percentage);

        if (occurredAt < LastUpdatedAt)
        {
            return;
        }

        Tier = tier.Trim();
        Currency = currency.ToUpperInvariant();
        Percentage = percentage;
        IsActive = true;
        LastUpdatedAt = occurredAt;
        MarkUpdated();
    }

    public void Deactivate(DateTime occurredAt)
    {
        if (!IsActive)
        {
            return;
        }
        IsActive = false;
        LastUpdatedAt = occurredAt;
        MarkUpdated();
    }

    private void MarkUpdated() => UpdatedAt = DateTime.UtcNow;

    private static void Validate(Guid ruleId, string tier, string currency, decimal percentage)
    {
        if (ruleId == Guid.Empty)
        {
            throw new ArgumentException("RuleId is required.", nameof(ruleId));
        }
        if (string.IsNullOrWhiteSpace(tier) || tier.Trim().Length > 50)
        {
            throw new ArgumentException("Tier is required and must be <= 50 chars.", nameof(tier));
        }
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        }
        if (percentage <= 0m || percentage >= 100m)
        {
            throw new ArgumentException("Percentage must be 0 < p < 100.", nameof(percentage));
        }
    }
}
