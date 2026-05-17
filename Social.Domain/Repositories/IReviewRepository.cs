using Social.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Social.Domain.Repositories;

public interface IReviewRepository : IRepository<Review, Guid>
{
    Task<IReadOnlyList<Review>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetByPlaceIdAsync(Guid placeId, CancellationToken ct = default);
    Task<IReadOnlyList<Review>> GetByTourIdAsync(Guid tourId, CancellationToken ct = default);
    Task<bool> ExistsForUserAndTargetAsync(Guid userId, Guid? placeId, Guid? tourId, Guid? tourGuideId, Guid? businessId, CancellationToken ct = default);
}
