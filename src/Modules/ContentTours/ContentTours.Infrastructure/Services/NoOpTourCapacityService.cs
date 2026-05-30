using ContentTours.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ContentTours.Infrastructure.Services;

internal sealed class NoOpTourCapacityService(
    ILogger<NoOpTourCapacityService> logger) : ITourCapacityService
{
    public Task<bool> AllHaveCapacityAsync(
        IReadOnlyCollection<Guid> tourIds,
        int? requiredSlots,
        CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "No booking module registered; assuming capacity OK for {TourCount} tours (requiredSlots={RequiredSlots}).",
            tourIds.Count, requiredSlots);
        return Task.FromResult(true);
    }
}
