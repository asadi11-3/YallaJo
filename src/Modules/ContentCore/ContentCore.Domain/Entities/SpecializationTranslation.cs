using ContentCore.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class SpecializationTranslation : BaseEntity
{
    private SpecializationTranslation() { } // EF Core

    public Guid SpecializationId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TranslationStatus Status { get; private set; } = TranslationStatus.AutoTranslated;

    public Specialization Specialization { get; private set; } = default!;

    public static SpecializationTranslation Create(
        Guid specializationId,
        Guid languageId,
        string name,
        string? description,
        TranslationStatus status = TranslationStatus.AutoTranslated)
    {
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language is required.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translation name is required.", nameof(name));

        return new SpecializationTranslation
        {
            SpecializationId = specializationId,
            LanguageId = languageId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Status = status
        };
    }

    public void Update(string name, string? description, TranslationStatus? status = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Translation name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();

        if (status.HasValue)
            Status = status.Value;
    }
}
