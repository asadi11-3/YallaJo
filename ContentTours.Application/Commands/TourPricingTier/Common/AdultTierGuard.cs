using ContentTours.Domain.Entities;
using ContentTours.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentTours.Application.Commands.TourPricingTier.Common;

/// <summary>
/// Encapsulates the "Adult tier is magic" business rule.
/// Cannot delete or deactivate the last active Adult tier on a Pending or Approved tour.
/// Allowed on Draft tours (provider still editing).
/// </summary>
internal static class AdultTierGuard
{
    public static Result EnsureCanRemoveOrDeactivate(
        ContentTours.Domain.Entities.Tour tour,
        ContentTours.Domain.Entities.TourPricingTier targetTier,
        IReadOnlyCollection<ContentTours.Domain.Entities.TourPricingTier> allTiersInTour)
    {
        if (!targetTier.IsAdult) return Result.Success();

        // Editable-state tours (Draft): always allowed — provider is still configuring
        if (tour.Status.IsEditable()) return Result.Success();

        // Check if any OTHER active Adult tier remains
        var remainingActiveAdult = allTiersInTour
            .Any(t => t.Id != targetTier.Id && t.IsActive && t.IsAdult);

        if (remainingActiveAdult) return Result.Success();

        return Result.Fail(
            Outcome.Conflict,
            new Error("TourPricingTier.AdultTierRequired",
                "Cannot delete or deactivate the last active Adult tier on a Pending or Approved tour."));
    }
}
