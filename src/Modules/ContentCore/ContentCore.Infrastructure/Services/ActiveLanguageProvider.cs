using ContentCore.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Queries the Language table for active languages.
/// Other modules inject <see cref="IActiveLanguageProvider"/> to discover
/// which languages they should auto-translate into.
/// </summary>
internal sealed class ActiveLanguageProvider(ILanguageRepository languageRepository) : IActiveLanguageProvider
{
    public async Task<IReadOnlyList<ActiveLanguage>> GetActiveLanguagesAsync(CancellationToken ct = default)
    {
        var languages = await languageRepository.GetAllAsync(
            filter: l => l.IsActive,
            ct: ct);

        return languages
            .Select(l => new ActiveLanguage(l.Id, l.Code))
            .ToList();
    }
}
