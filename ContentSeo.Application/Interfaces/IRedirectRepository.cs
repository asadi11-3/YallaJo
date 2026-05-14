using ContentSeo.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentSeo.Application.Interfaces;

/// <summary>
/// Repository for the <see cref="Redirect"/> aggregate root.
/// Compile-only stub for Wave-4 pre-work — chain-flatten / lookup-by-old-url
/// query methods will be added during TASK 3 implementation.
/// </summary>
public interface IRedirectRepository : IRepository<Redirect>
{
    Task<Redirect?> GetActiveByOldUrlAsync(string oldUrl,CancellationToken ct =default);

    Task<IReadOnlyList<Redirect>> GetActivePointingToAsync(string targetUrl, CancellationToken ct = default);
}
