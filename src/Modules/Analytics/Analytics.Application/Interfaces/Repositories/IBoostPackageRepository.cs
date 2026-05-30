using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IBoostPackageRepository : IRepository<BoostPackage, Guid>
{
    Task<BoostPackage?> GetActiveForEntityAsync(EntityType kind, Guid entityId, CancellationToken ct = default);
    Task<IReadOnlyList<BoostPackage>> GetActiveByProviderAsync(Guid providerId, CancellationToken ct = default);
    Task<IReadOnlyList<BoostPackage>> GetAllActiveCpcBidsAsync(CancellationToken ct = default);
}
