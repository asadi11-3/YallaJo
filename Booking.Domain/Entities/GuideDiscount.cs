using Booking.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

/// <summary>
/// A per-offering discount created by a guide to promote their tour slots.
/// Separate from Finance.Discount which handles platform-level discounts.
/// Max 2 stackable discounts per booking (tour-auto + guide promotion).
/// </summary>
public sealed class GuideDiscount : AuditableEntity, IAggregateRoot
{
    private GuideDiscount() { } // EF Core

    /// <summary>TourGuide userId from ContentTours.</summary>
    public Guid GuideUserId { get; private set; }

    /// <summary>Specific tour this discount applies to (null = applies to all guide's tours).</summary>
    public Guid? TourId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public GuideDiscountType DiscountType { get; private set; }

    /// <summary>Percentage (0-100) for Percentage type, or fixed JOD amount for FixedAmount type.</summary>
    public decimal DiscountValue { get; private set; }

    public string Currency { get; private set; } = "JOD";

    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }

    /// <summary>Maximum number of times this discount can be used (null = unlimited).</summary>
    public int? MaxUsageCount { get; private set; }
    public int CurrentUsageCount { get; private set; }

    public bool IsActive { get; private set; } = true;

    // === Factory ===

    public static Result<GuideDiscount> Create(
        Guid guideUserId,
        Guid? tourId,
        string name,
        string? description,
        GuideDiscountType discountType,
        decimal discountValue,
        string currency,
        DateTime validFrom,
        DateTime? validUntil,
        int? maxUsageCount,
        DateTime utcNow)
    {
        if (discountType == GuideDiscountType.Percentage && (discountValue <= 0m || discountValue > 100m))
        {
            return Result.Failure<GuideDiscount>(Error.Validation("GuideDiscount.InvalidPercentage", "Percentage must be between 0 and 100."));
        }

        if (discountType == GuideDiscountType.FixedAmount && discountValue <= 0m)
        {
            return Result.Failure<GuideDiscount>(Error.Validation("GuideDiscount.InvalidAmount", "Fixed amount must be positive."));
        }

        if (validUntil.HasValue && validUntil.Value <= validFrom)
        {
            return Result.Failure<GuideDiscount>(Error.Validation("GuideDiscount.InvalidDateRange", "ValidUntil must be after ValidFrom."));
        }

        var discount = new GuideDiscount
        {
            Id = Guid.NewGuid(),
            GuideUserId = guideUserId,
            TourId = tourId,
            Name = name.Trim(),
            Description = description?.Trim(),
            DiscountType = discountType,
            DiscountValue = discountValue,
            Currency = currency,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            MaxUsageCount = maxUsageCount,
            CurrentUsageCount = 0,
            IsActive = true,
            CreatedAt = utcNow,
        };

        return Result.Success(discount);
    }

    // === Domain methods ===

    public Result Update(
        string name,
        string? description,
        decimal discountValue,
        DateTime validFrom,
        DateTime? validUntil,
        int? maxUsageCount)
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Conflict("GuideDiscount.Inactive", "Cannot update an inactive discount."));
        }

        if (DiscountType == GuideDiscountType.Percentage && (discountValue <= 0m || discountValue > 100m))
        {
            return Result.Failure(Error.Validation("GuideDiscount.InvalidPercentage", "Percentage must be between 0 and 100."));
        }

        if (DiscountType == GuideDiscountType.FixedAmount && discountValue <= 0m)
        {
            return Result.Failure(Error.Validation("GuideDiscount.InvalidAmount", "Fixed amount must be positive."));
        }

        Name = name.Trim();
        Description = description?.Trim();
        DiscountValue = discountValue;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        MaxUsageCount = maxUsageCount;
        MarkUpdated();
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Conflict("GuideDiscount.AlreadyInactive", "Discount is already inactive."));
        }

        IsActive = false;
        MarkUpdated();
        return Result.Success();
    }

    public Result IncrementUsage()
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Conflict("GuideDiscount.Inactive", "Discount is not active."));
        }

        if (MaxUsageCount.HasValue && CurrentUsageCount >= MaxUsageCount.Value)
        {
            return Result.Failure(Error.Conflict("GuideDiscount.UsageLimitReached", "This discount has reached its maximum usage limit."));
        }

        CurrentUsageCount++;
        MarkUpdated();
        return Result.Success();
    }

    public bool IsValidAt(DateTime utcNow) =>
        IsActive &&
        utcNow >= ValidFrom &&
        (!ValidUntil.HasValue || utcNow <= ValidUntil.Value) &&
        (!MaxUsageCount.HasValue || CurrentUsageCount < MaxUsageCount.Value);
}
