using Booking.Domain.ValueObjects;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class RefundPolicy : AuditableEntity, IAggregateRoot
{
    public const int MaxTierCount = 10;
    public static IReadOnlyList<RefundTier> DefaultTiers { get; } =
        new[]
        {
            new RefundTier(24, 100m),
            new RefundTier(0, 0m),
        };

    private readonly List<RefundTier> _tiers = new();

    private RefundPolicy() { } // EF Core

    public Guid TourId { get; private set; }

    public IReadOnlyList<RefundTier> Tiers => _tiers.AsReadOnly();

    public bool IsActive { get; private set; } = true;

    public static RefundPolicy Create(Guid tourId, IEnumerable<RefundTier> tiers)
    {
        if (tourId == Guid.Empty)
        {
            throw new BusinessRuleViolationException("TourId is required.");
        }

        var normalized = NormalizeTiers(tiers);
        var policy = new RefundPolicy
        {
            TourId = tourId,
            IsActive = true,
        };
        policy._tiers.AddRange(normalized);
        return policy;
    }

    public void Update(IEnumerable<RefundTier> tiers)
    {
        var normalized = NormalizeTiers(tiers);
        _tiers.Clear();
        _tiers.AddRange(normalized);
        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }
        IsActive = false;
        MarkUpdated();
    }

    public decimal CalculateRefundPercentage(TimeSpan timeUntilTour)
        => CalculateRefundPercentage(_tiers, timeUntilTour);

    public static decimal CalculateRefundPercentage(IEnumerable<RefundTier> tiers, TimeSpan timeUntilTour)
    {
        ArgumentNullException.ThrowIfNull(tiers);

        var hoursRemaining = timeUntilTour.TotalHours;
        var winnerThreshold = int.MinValue;
        var winnerPct = 0m;

        foreach (var tier in tiers)
        {
            if (hoursRemaining >= tier.HoursBeforeTour && tier.HoursBeforeTour > winnerThreshold)
            {
                winnerThreshold = tier.HoursBeforeTour;
                winnerPct = Math.Clamp(tier.RefundPercent, 0m, 100m);
            }
        }

        return winnerThreshold == int.MinValue ? 0m : winnerPct;
    }

    private static List<RefundTier> NormalizeTiers(IEnumerable<RefundTier> tiers)
    {
        ArgumentNullException.ThrowIfNull(tiers);

        var list = tiers.ToList();
        if (list.Count == 0)
        {
            throw new BusinessRuleViolationException("RefundPolicy must have at least one tier.");
        }

        if (list.Count > MaxTierCount)
        {
            throw new BusinessRuleViolationException(
                $"RefundPolicy cannot have more than {MaxTierCount} tiers.");
        }

        foreach (var tier in list)
        {
            if (tier is null)
            {
                throw new BusinessRuleViolationException("RefundPolicy tier cannot be null.");
            }
            if (tier.HoursBeforeTour < 0)
            {
                throw new BusinessRuleViolationException(
                    "RefundPolicy tier HoursBeforeTour must be >= 0.");
            }
            if (tier.RefundPercent < 0m || tier.RefundPercent > 100m)
            {
                throw new BusinessRuleViolationException(
                    "RefundPolicy tier RefundPercent must be between 0 and 100.");
            }
        }

        var distinctHours = list.Select(t => t.HoursBeforeTour).Distinct().Count();
        if (distinctHours != list.Count)
        {
            throw new BusinessRuleViolationException(
                "RefundPolicy tiers must have unique HoursBeforeTour values.");
        }

        return list.OrderByDescending(t => t.HoursBeforeTour).ToList();
    }
}
