using Booking.Application.Interfaces;

namespace Booking.Infrastructure.Services;

/// <summary>
/// STUB implementation of <see cref="IDiscountEvaluator"/>.
/// </summary>
/// <remarks>
/// TODO: Replace with a real evaluator once the Promotions module is built.
/// Until then this returns <see cref="DiscountEvaluationResult.None"/> for every
/// request so the booking flow proceeds with zero discount applied.
/// </remarks>
internal sealed class NoOpDiscountEvaluator : IDiscountEvaluator
{
    public Task<DiscountEvaluationResult> EvaluateAsync(
        DiscountEvaluationContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult(DiscountEvaluationResult.None);
}
