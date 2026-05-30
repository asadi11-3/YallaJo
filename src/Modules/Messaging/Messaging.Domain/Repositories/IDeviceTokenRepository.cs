using Messaging.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Messaging.Domain.Repositories;

/// <summary>Repository for DeviceToken aggregate queries.</summary>
public interface IDeviceTokenRepository : IRepository<DeviceToken, Guid>
{
    Task<IReadOnlyList<DeviceToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<DeviceToken?> GetByUserAndDeviceIdAsync(Guid userId, string deviceId, CancellationToken ct = default);
    Task<IReadOnlyList<DeviceToken>> GetStaleAsync(DateTime olderThan, int batchSize, CancellationToken ct = default);
}
