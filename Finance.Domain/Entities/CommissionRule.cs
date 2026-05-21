using Finance.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace Finance.Domain.Entities;

/// <summary>
/// Aggregate that captures the commission percentage charged by the platform
/// for a given provider subscription tier within a (Currency, monthly-revenue) range.
/// F-R6 lookup walks: Tier override → CommissionRule for (Tier, Currency) → fallback 15%.
/// </summary>
public sealed class CommissionRule : AuditableEntity, IAggregateRoot
{
    private CommissionRule() { } // EF Core

    /// <summary>Provider subscription tier (free text v1: Free/Basic/Premium/Enterprise; max 50).</summary>
    public string Tier { get; private set; } = string.Empty;

    /// <summary>Minimum monthly revenue (inclusive) for this rule to apply.</summary>
    public decimal MinMonthlyRevenue { get; private set; }

    /// <summary>Maximum monthly revenue (exclusive). Null means "open top tier".</summary>
    public decimal? MaxMonthlyRevenue { get; private set; }

    /// <summary>3-letter ISO currency code (JOD/USD/EUR this sprint).</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>Commission percentage 0 &lt; p &lt; 100.</summary>
    public decimal Percentage { get; private set; }

    /// <summary>True if rule is active (used by lookup; soft-deletes flip this to false).</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Free-form notes for admin context.</summary>
    public string? Notes { get; private set; }

    public static CommissionRule Create(
        string tier,
        decimal minMonthlyRevenue,
        decimal? maxMonthlyRevenue,
        string currency,
        decimal percentage,
        string? notes,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Validate(tier, minMonthlyRevenue, maxMonthlyRevenue, currency, percentage);

        var rule = new CommissionRule
        {
            Tier = tier.Trim(),
            MinMonthlyRevenue = minMonthlyRevenue,
            MaxMonthlyRevenue = maxMonthlyRevenue,
            Currency = currency.ToUpperInvariant(),
            Percentage = percentage,
            Notes = notes?.Trim(),
            IsActive = true,
        };
        rule.AddDomainEvent(new CommissionRuleUpsertedDomainEvent(
            rule.Id, rule.Tier, rule.MinMonthlyRevenue, rule.MaxMonthlyRevenue, rule.Currency, rule.Percentage));
        return rule;
    }

    public void Update(
        decimal minMonthlyRevenue,
        decimal? maxMonthlyRevenue,
        decimal percentage,
        string? notes)
    {
        Validate(Tier, minMonthlyRevenue, maxMonthlyRevenue, Currency, percentage);
        MinMonthlyRevenue = minMonthlyRevenue;
        MaxMonthlyRevenue = maxMonthlyRevenue;
        Percentage = percentage;
        Notes = notes?.Trim();
        AddDomainEvent(new CommissionRuleUpsertedDomainEvent(
            Id, Tier, MinMonthlyRevenue, MaxMonthlyRevenue, Currency, Percentage));
        MarkUpdated();
    }

    public void Deactivate(string? reason)
    {
        if (!IsActive)
        {
            return; // idempotent
        }

        IsActive = false;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : $"{Notes} | Deactivated: {reason}";
        SoftDelete();
        AddDomainEvent(new CommissionRuleDeletedDomainEvent(Id));
    }

    private static void Validate(string tier, decimal min, decimal? max, string currency, decimal percentage)
    {
        if (string.IsNullOrWhiteSpace(tier) || tier.Trim().Length > 50)
        {
            throw new ArgumentException("Tier is required and must be <= 50 chars.", nameof(tier));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        }

        if (min < 0)
        {
            throw new ArgumentException("MinMonthlyRevenue must be >= 0.", nameof(min));
        }

        if (max.HasValue && max.Value <= min)
        {
            throw new ArgumentException("MaxMonthlyRevenue must be > MinMonthlyRevenue or null.", nameof(max));
        }

        if (percentage <= 0m || percentage >= 100m)
        {
            throw new ArgumentException("Percentage must be 0 < p < 100.", nameof(percentage));
        }
    }
}
