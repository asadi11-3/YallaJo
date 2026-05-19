namespace Booking.Application.Interfaces;

/// <summary>
/// Input for discount evaluation during the pricing step of booking creation.
/// </summary>
public sealed record DiscountEvaluationContext(
    Guid UserId,
    Guid TourId,
    Guid ProviderId,
    decimal Subtotal,
    string Currency,
    string? PromoCode);

/// <summary>
/// The result of evaluating a discount. AppliedAmount must always be >= 0 and <= Subtotal.
/// </summary>
public sealed record DiscountEvaluationResult(
    decimal AppliedAmount,
    string? PromoCodeApplied,
    string? Description)
{
    public static DiscountEvaluationResult None { get; } = new(0m, null, null);
}

/// <summary>
/// Evaluates discounts (promo codes, campaign rules, etc.) for a tentative booking.
/// </summary>
public interface IDiscountEvaluator
{
    Task<DiscountEvaluationResult> EvaluateAsync(
        DiscountEvaluationContext context,
        CancellationToken cancellationToken = default);
}
