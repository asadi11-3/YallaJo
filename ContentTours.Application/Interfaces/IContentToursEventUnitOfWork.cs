namespace ContentTours.Application.Interfaces;

/// <summary>
/// Event-dispatching unit of work for ContentTours.
/// Dispatches domain events from IAggregateRoot entries BEFORE SaveChanges.
/// Use this for commands that mutate Tour aggregate (e.g., ToggleTourFeatured).
/// For non-aggregate entities (TourSchedule, TourPricingTier), use <see cref="IContentToursUnitOfWork"/>.
/// </summary>
public interface IContentToursEventUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
