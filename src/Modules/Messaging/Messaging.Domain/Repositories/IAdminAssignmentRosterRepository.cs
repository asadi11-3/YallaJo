using Messaging.Domain.Entities;

namespace Messaging.Domain.Repositories;

/// <summary>Repository for round-robin admin assignment roster.</summary>
public interface IAdminAssignmentRosterRepository
{
    /// <summary>Atomically picks the next available admin (oldest LastAssignedAt, IsActive, !IsOnLeave).</summary>
    Task<AdminAssignmentRoster?> PickNextAdminAsync(CancellationToken ct = default);
    Task AddAsync(AdminAssignmentRoster entry, CancellationToken ct = default);
    Task<IReadOnlyList<AdminAssignmentRoster>> GetActiveAsync(CancellationToken ct = default);
}
