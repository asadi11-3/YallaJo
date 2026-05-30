using ContentCore.Domain.Enums;
using ContentCore.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Specialization : AuditableEntity, IAggregateRoot
{
    private readonly List<SpecializationTranslation> _translations = [];

    private Specialization() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Icon { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string SourceLanguageCode { get; private set; } = "en";

    public IReadOnlyCollection<SpecializationTranslation> Translations => _translations.AsReadOnly();

    public static Specialization Create(
        string name,
        string? description = null,
        string? icon = null,
        string sourceLanguageCode = "en")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Specialization name cannot be empty.", nameof(name));

        var spec = new Specialization
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Icon = icon?.Trim(),
            IsActive = true,
            SourceLanguageCode = sourceLanguageCode.Trim().ToLowerInvariant()
        };

        spec.AddDomainEvent(new SpecializationCreatedDomainEvent(
            spec.Id, spec.Name, spec.Description, spec.SourceLanguageCode));

        return spec;
    }

    public void Update(string name, string? description, string? icon, string sourceLanguageCode = "en")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Specialization name cannot be empty.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        Icon = icon?.Trim();
        SourceLanguageCode = sourceLanguageCode.Trim().ToLowerInvariant();
        MarkUpdated();

        AddDomainEvent(new SpecializationUpdatedDomainEvent(
            Id, Name, Description, SourceLanguageCode));
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

    // ── Translation helpers ──
    public void AddTranslation(Guid languageId, string name, string? description)
    {
        if (_translations.Any(t => t.LanguageId == languageId))
            throw new InvalidOperationException("Translation already exists for this language.");

        _translations.Add(SpecializationTranslation.Create(Id, languageId, name, description));
    }

    public bool TryUpdateAutoTranslation(Guid languageId, string name, string? description)
    {
        var translation = _translations.FirstOrDefault(t => t.LanguageId == languageId);
        if (translation is null) return false;
        if (translation.Status == TranslationStatus.HumanReviewed) return false;

        translation.Update(name, description, TranslationStatus.AutoTranslated);
        return true;
    }
}
