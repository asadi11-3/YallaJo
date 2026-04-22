using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

public interface IExternalProviderRepository : IRepository<ExternalProvider, Guid>
{
    /// <summary>
    /// Returns the active external-provider link that matches the normalized
    /// provider name and provider-assigned user identifier, or <c>null</c> if
    /// no active link exists.
    /// </summary>
    Task<ExternalProvider?> FindActiveLinkAsync(
        string normalizedProvider,
        string providerUserId,
        CancellationToken ct = default);
}
