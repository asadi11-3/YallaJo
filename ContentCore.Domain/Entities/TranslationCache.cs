using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

/// <summary>
/// Generic text-to-text translation cache. Stores results from external
/// translation API calls so the same text is never translated twice.
/// Entity-specific translations (CategoryTranslation, PlaceTranslation, etc.)
/// are separate — this is the raw API result cache used by <c>AutoSaveTranslationService</c>.
/// </summary>
public sealed class TranslationCache : BaseEntity, IAggregateRoot
{
    private TranslationCache() { } // EF Core

    public string OriginalText { get; private set; } = string.Empty;
    public string TranslatedText { get; private set; } = string.Empty;
    public string FromLanguage { get; private set; } = string.Empty;
    public string ToLanguage { get; private set; } = string.Empty;
    public double? Confidence { get; private set; }
    public TranslationStatus Status { get; private set; } = TranslationStatus.AutoTranslated;

    /// <summary>
    /// Optional link to a specific entity whose field was translated.
    /// Null for on-demand (admin tool) translations.
    /// </summary>
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? FieldName { get; private set; }

    public static TranslationCache Create(
        string originalText,
        string translatedText,
        string fromLanguage,
        string toLanguage,
        double? confidence,
        string? entityType = null,
        Guid? entityId = null,
        string? fieldName = null)
    {
        return new TranslationCache
        {
            OriginalText = originalText,
            TranslatedText = translatedText,
            FromLanguage = fromLanguage.ToLowerInvariant(),
            ToLanguage = toLanguage.ToLowerInvariant(),
            Confidence = confidence,
            Status = TranslationStatus.AutoTranslated,
            EntityType = entityType,
            EntityId = entityId,
            FieldName = fieldName
        };
    }

    public void UpdateTranslation(string translatedText)
    {
        TranslatedText = translatedText;
        Status = TranslationStatus.HumanReviewed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        Status = TranslationStatus.HumanReviewed;
        UpdatedAt = DateTime.UtcNow;
    }
}
