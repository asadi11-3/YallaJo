using Accounts.Domain.Entities;
using Accounts.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Accounts.Domain.Repositories;

public interface IAgencyInvitationRepository : IRepository<AgencyInvitation, Guid>
{
    Task<AgencyInvitation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<AgencyInvitation>> GetByAgencyUserIdAsync(
        Guid agencyUserId,
        AgencyInvitationStatus? statusFilter,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgencyInvitation>> GetByGuideUserIdAsync(
        Guid guideUserId,
        AgencyInvitationStatus? statusFilter,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgencyInvitation>> GetPendingExpiredAsync(
        DateTime now,
        CancellationToken ct = default);

    Task<bool> HasPendingInvitationAsync(
        Guid agencyUserId,
        Guid guideUserId,
        CancellationToken ct = default);
}
