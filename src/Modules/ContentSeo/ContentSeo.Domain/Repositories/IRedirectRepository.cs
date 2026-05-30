using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Domain.Repositories;

/// <summary>
/// Repository for the <see cref="Redirect"/> aggregate root.
/// </summary>
public interface IRedirectRepository : IRepository<Redirect>
{
    Task<Redirect?> GetActiveByOldUrlAsync(string oldUrl, CancellationToken ct = default);

    Task<IReadOnlyList<Redirect>> GetActivePointingToAsync(string targetUrl, CancellationToken ct = default);
}
