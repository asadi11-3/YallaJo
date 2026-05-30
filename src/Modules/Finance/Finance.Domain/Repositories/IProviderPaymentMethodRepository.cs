using Finance.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Finance.Domain.Repositories;

public interface IProviderPaymentMethodRepository : IRepository<ProviderPaymentMethod, Guid>
{
    Task<IReadOnlyList<ProviderPaymentMethod>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<ProviderPaymentMethod>> GetByUserIdTrackedAsync(Guid userId, CancellationToken ct = default);

    Task<ProviderPaymentMethod?> GetDefaultForProviderAsync(Guid userId, CancellationToken ct = default);

    Task<bool> ExistsForProviderAsync(Guid userId, string accountIdentifier, CancellationToken ct = default);
}
