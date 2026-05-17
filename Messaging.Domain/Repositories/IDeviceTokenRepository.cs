using Messaging.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

public interface IDeviceTokenRepository : IRepository<DeviceToken, Guid>
{
    Task<IReadOnlyList<DeviceToken>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken ct = default);
}
