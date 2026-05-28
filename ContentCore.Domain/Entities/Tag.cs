using ContentCore.Domain.Enums;
using ContentCore.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Tag : AuditableEntity, IAggregateRoot
{
    private readonly List<TagTranslation> _translations = [];

    private Tag() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public string SourceLanguageCode { get; private set; } = "en";

    public IReadOnlyCollection<TagTranslation> Translations => _translations.AsReadOnly();

    // ── Factory Method ──
    public static Tag Create(string name, string slug, string sourceLanguageCode = "en")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tag slug is required.", nameof(slug));

        var tag = new Tag
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            IsActive = true,
            SourceLanguageCode = sourceLanguageCode.Trim().ToLowerInvariant()
        };

        tag.AddDomainEvent(new TagCreatedDomainEvent(tag.Id, tag.Name, tag.SourceLanguageCode));

        return tag;
    }

    // ── Business Methods ──
    public void Update(string name, string slug, string sourceLanguageCode = "en")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Tag slug is required.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        SourceLanguageCode = sourceLanguageCode.Trim().ToLowerInvariant();
        MarkUpdated();

        AddDomainEvent(new TagUpdatedDomainEvent(Id, Name, SourceLanguageCode));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        MarkUpdated();
    }

    // ── Translation helpers (mirror Category pattern) ──
    public void AddTranslation(Guid languageId, string name, string slug)
    {
        if (_translations.Any(t => t.LanguageId == languageId))
            throw new InvalidOperationException("Translation already exists for this language.");

        _translations.Add(TagTranslation.Create(Id, languageId, name, slug));
    }

    public bool TryUpdateAutoTranslation(Guid languageId, string name, string slug)
    {
        var translation = _translations.FirstOrDefault(t => t.LanguageId == languageId);
        if (translation is null) return false;
        if (translation.Status == TranslationStatus.HumanReviewed) return false;

        translation.Update(name, slug, TranslationStatus.AutoTranslated);
        return true;
    }

    public static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.Trim().ToLowerInvariant().Replace(' ', '-'), @"[^a-z0-9\-]", string.Empty)
            .Trim('-');
}
