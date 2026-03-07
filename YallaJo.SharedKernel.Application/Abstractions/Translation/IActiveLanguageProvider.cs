namespace YallaJo.SharedKernel.Application.Abstractions.Translation;

/// <summary>
/// Provides the set of currently active languages in the platform.
/// Implemented by the ContentCore module; consumed by any module
/// that needs to auto-translate content into all active languages.
/// </summary>
public interface IActiveLanguageProvider
{
    Task<IReadOnlyList<ActiveLanguage>> GetActiveLanguagesAsync(CancellationToken ct = default);
}

/// <summary>
/// Lightweight projection of a Language entity carrying only the data
/// other modules need for translation workflows.
/// </summary>
public sealed record ActiveLanguage(Guid Id, string Code);
