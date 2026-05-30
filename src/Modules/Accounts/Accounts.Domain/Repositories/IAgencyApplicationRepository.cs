using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Accounts.Domain.Repositories;

public interface IAgencyApplicationRepository : IRepository<AgencyApplication, Guid>
{
    Task<AgencyApplication?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<AgencyApplication>> GetByAgencyUserIdAsync(
        Guid agencyUserId,
        AgencyApplicationStatus? statusFilter,
        CancellationToken ct = default);

    Task<bool> HasPendingApplicationAsync(
        Guid guideUserId,
        Guid agencyUserId,
        CancellationToken ct = default);
}
