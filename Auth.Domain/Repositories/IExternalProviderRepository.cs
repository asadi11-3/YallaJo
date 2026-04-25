using Auth.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Auth.Domain.Repositories;

public interface IExternalProviderRepository : IRepository<ExternalProvider, Guid>
{
Task<ExternalProvider?> FindActiveLinkAsync(string normalizedProvider, string providerUserId, CancellationToken ct = default);
}
