namespace ContentTours.Contracts.Tours;

public interface ITourExistenceService
{
    Task<TourExistenceStatus> GetStatusAsync(
        Guid tourId,
        CancellationToken cancellationToken = default);
}
