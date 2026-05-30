namespace ContentTours.Application.Interfaces;

public interface ITourCapacityService
{
    Task<bool> AllHaveCapacityAsync(
        IReadOnlyCollection<Guid> tourIds,
        int? requiredSlots,
        CancellationToken cancellationToken);
}
