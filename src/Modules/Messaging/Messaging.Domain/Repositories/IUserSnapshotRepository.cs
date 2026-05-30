using Messaging.Domain.Entities;

namespace Messaging.Domain.Repositories;

/// <summary>Read-only cross-module user snapshot (email, name, language) for notification delivery.</summary>
public interface IUserSnapshotRepository
{
    Task<UserSnapshot?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserSnapshot snapshot, CancellationToken ct = default);
}
