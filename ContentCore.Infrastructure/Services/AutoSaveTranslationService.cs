using ContentCore.Application.Interfaces;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Repositories;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Translation;

namespace ContentCore.Infrastructure.Services;

/// <summary>
/// Decorates any <see cref="ITranslationService"/> implementation to auto-persist
/// translation results to the database. On subsequent calls with the same input,
/// the cached result is returned without hitting the external API.
/// </summary>
public sealed class AutoSaveTranslationService : ITranslationService
{
    private readonly ITranslationService _inner;
    private readonly ITranslationCacheRepository _cacheRepository;
    private readonly IContentCoreUnitOfWork _unitOfWork;
    private readonly ILogger<AutoSaveTranslationService> _logger;

    public AutoSaveTranslationService(
        ITranslationService inner,
        ITranslationCacheRepository cacheRepository,
        IContentCoreUnitOfWork unitOfWork,
        ILogger<AutoSaveTranslationService> logger)
    {
        _inner = inner;
        _cacheRepository = cacheRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string fromLanguageCode,
        string toLanguageCode,
        CancellationToken ct = default)
    {
        // 1. Check DB cache first
        var cached = await _cacheRepository.FindCachedAsync(text, fromLanguageCode, toLanguageCode, ct);
        if (cached is not null)
        {
            _logger.LogDebug(
                "Translation cache hit: '{Text}' {From} → {To}",
                text[..Math.Min(50, text.Length)], fromLanguageCode, toLanguageCode);
            return new TranslationResult(
                cached.OriginalText,
                cached.TranslatedText,
                cached.FromLanguage,
                cached.ToLanguage,
                cached.Confidence);
        }

        // 2. Call external API
        var result = await _inner.TranslateAsync(text, fromLanguageCode, toLanguageCode, ct);

        // 3. Auto-save to DB
        var entry = TranslationCache.Create(
            result.OriginalText,
            result.TranslatedText,
            result.FromLanguage,
            result.ToLanguage,
            result.Confidence);

        await _cacheRepository.AddAsync(entry, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return result;
    }

    public async Task<IReadOnlyList<TranslationResult>> BatchTranslateAsync(
        IReadOnlyList<string> texts,
        string fromLanguageCode,
        string toLanguageCode,
        CancellationToken ct = default)
    {
        var results = new List<TranslationResult>(texts.Count);
        var uncachedTexts = new List<string>();
        var uncachedIndices = new List<int>();

        // 1. Check cache for each text
        for (var i = 0; i < texts.Count; i++)
        {
            var cached = await _cacheRepository.FindCachedAsync(texts[i], fromLanguageCode, toLanguageCode, ct);
            if (cached is not null)
            {
                results.Add(new TranslationResult(
                    cached.OriginalText,
                    cached.TranslatedText,
                    cached.FromLanguage,
                    cached.ToLanguage,
                    cached.Confidence));
            }
            else
            {
                results.Add(null!); // placeholder
                uncachedTexts.Add(texts[i]);
                uncachedIndices.Add(i);
            }
        }

        // 2. Batch-translate uncached texts
        if (uncachedTexts.Count > 0)
        {
            var apiResults = await _inner.BatchTranslateAsync(uncachedTexts, fromLanguageCode, toLanguageCode, ct);

            for (var i = 0; i < apiResults.Count; i++)
            {
                var r = apiResults[i];
                results[uncachedIndices[i]] = r;

                // 3. Auto-save each result
                var entry = TranslationCache.Create(
                    r.OriginalText,
                    r.TranslatedText,
                    r.FromLanguage,
                    r.ToLanguage,
                    r.Confidence);
                await _cacheRepository.AddAsync(entry, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
        }

        _logger.LogDebug(
            "Batch translate: {Total} total, {Cached} cached, {Api} API calls",
            texts.Count, texts.Count - uncachedTexts.Count, uncachedTexts.Count);

        return results;
    }

    public Task<string> DetectLanguageAsync(string text, CancellationToken ct = default)
        => _inner.DetectLanguageAsync(text, ct);

    public Task<IReadOnlyList<SupportedLanguage>> GetSupportedLanguagesAsync(CancellationToken ct = default)
        => _inner.GetSupportedLanguagesAsync(ct);
}
