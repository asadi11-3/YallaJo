using ContentTours.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentTours.Domain.Repositories;

public interface ITourRepository : IRepository<Tour, Guid>
{
    Task<bool> IsSlugReservedAsync(string slug, Guid? excludeTourId, CancellationToken ct = default);

    Task<bool> IsNameTakenByProviderAsync(string name, Guid providerUserId, Guid? excludeTourId, CancellationToken ct = default);

    Task<bool> HasFutureSchedulesAsync(Guid tourId, CancellationToken ct = default);

    Task<bool> HasActivePricingAsync(Guid tourId, CancellationToken ct = default);

    Task<bool> HasActiveAdultPricingAsync(Guid tourId, CancellationToken ct = default);

    Task<bool> HasActiveScheduleAsync(Guid tourId, CancellationToken ct = default);
    Task<IReadOnlyList<Tour>> GetByIdsIncludingDeletedAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);
}
