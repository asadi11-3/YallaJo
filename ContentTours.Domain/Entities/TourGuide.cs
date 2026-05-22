using ContentTours.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Domain.Entities;

public sealed class TourGuide : AuditableEntity, IAggregateRoot
{
    private static readonly HashSet<string> AllowedProficiencies =
        new(StringComparer.OrdinalIgnoreCase) { "Native", "Fluent", "Conversational", "Basic" };

    private readonly List<TourGuideLanguage> _languages = [];
    private readonly List<TourGuideSpecialization> _specializations = [];

    private TourGuide()
    {
    }

    public Guid UserId { get; private set; }
    public string Bio { get; private set; } = string.Empty;
    public int YearsOfExperience { get; private set; }
    public bool HasFirstAid { get; private set; }
    public string? MoTALicenseNumber { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<TourGuideLanguage> Languages => _languages.AsReadOnly();
    public IReadOnlyCollection<TourGuideSpecialization> Specializations => _specializations.AsReadOnly();

    public static TourGuide Register(
        Guid userId,
        string bio,
        int yearsOfExperience,
        bool hasFirstAid,
        string? moTALicenseNumber)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        ValidateProfile(bio, yearsOfExperience);

        var guide = new TourGuide
        {
            UserId = userId,
            Bio = bio.Trim(),
            YearsOfExperience = yearsOfExperience,
            HasFirstAid = hasFirstAid,
            MoTALicenseNumber = NormalizeOptional(moTALicenseNumber)
        };

        guide.AddDomainEvent(new TourGuideRegisteredDomainEvent(guide.Id, guide.UserId));

        return guide;
    }

    public void UpdateProfile(
        string bio,
        int yearsOfExperience,
        bool hasFirstAid,
        string? moTALicenseNumber)
    {
        EnsureActive();
        ValidateProfile(bio, yearsOfExperience);

        Bio = bio.Trim();
        YearsOfExperience = yearsOfExperience;
        HasFirstAid = hasFirstAid;
        MoTALicenseNumber = NormalizeOptional(moTALicenseNumber);
        MarkUpdated();

        AddDomainEvent(new TourGuideUpdatedDomainEvent(Id, UserId));
    }

    public void AddLanguage(Guid languageId, string proficiency)
    {
        EnsureActive();
        if (languageId == Guid.Empty)
        {
            throw new ArgumentException("LanguageId is required.", nameof(languageId));
        }

        var normalizedProficiency = NormalizeProficiency(proficiency);
        if (_languages.Any(language => language.LanguageId == languageId))
        {
            throw new InvalidOperationException("TourGuideLanguage.AlreadyExists: this language is already assigned to the guide.");
        }

        _languages.Add(TourGuideLanguage.Create(Id, languageId, normalizedProficiency));
        MarkUpdated();

        AddDomainEvent(new TourGuideLanguageAddedDomainEvent(Id, UserId, languageId, normalizedProficiency));
    }

    public void RemoveLanguage(Guid languageId)
    {
        EnsureActive();
        var language = _languages.FirstOrDefault(item => item.LanguageId == languageId);
        if (language is null)
        {
            throw new InvalidOperationException("TourGuideLanguage.NotFound: this language is not assigned to the guide.");
        }

        if (_languages.Count == 1)
        {
            throw new InvalidOperationException("TourGuideLanguage.LastLanguage: a guide must keep at least one language.");
        }

        _languages.Remove(language);
        MarkUpdated();

        AddDomainEvent(new TourGuideLanguageRemovedDomainEvent(Id, UserId, languageId));
    }

    public void AddSpecialization(Guid specializationId)
    {
        EnsureActive();
        if (specializationId == Guid.Empty)
        {
            throw new ArgumentException("SpecializationId is required.", nameof(specializationId));
        }

        if (_specializations.Any(specialization => specialization.SpecializationId == specializationId))
        {
            throw new InvalidOperationException("TourGuideSpecialization.AlreadyExists: this specialization is already assigned to the guide.");
        }

        _specializations.Add(TourGuideSpecialization.Create(Id, specializationId));
        MarkUpdated();

        AddDomainEvent(new TourGuideSpecializationAddedDomainEvent(Id, UserId, specializationId));
    }

    public void RemoveSpecialization(Guid specializationId)
    {
        EnsureActive();
        var specialization = _specializations.FirstOrDefault(item => item.SpecializationId == specializationId);
        if (specialization is null)
        {
            throw new InvalidOperationException("TourGuideSpecialization.NotFound: this specialization is not assigned to the guide.");
        }

        _specializations.Remove(specialization);
        MarkUpdated();
    }

    public static bool IsValidProficiency(string proficiency) =>
        !string.IsNullOrWhiteSpace(proficiency) && AllowedProficiencies.Contains(proficiency.Trim());

    private static void ValidateProfile(string bio, int yearsOfExperience)
    {
        if (string.IsNullOrWhiteSpace(bio))
        {
            throw new ArgumentException("Bio is required.", nameof(bio));
        }

        if (bio.Trim().Length > 2000)
        {
            throw new ArgumentException("Bio cannot exceed 2000 characters.", nameof(bio));
        }

        if (yearsOfExperience is < 0 or > 80)
        {
            throw new ArgumentOutOfRangeException(nameof(yearsOfExperience), "YearsOfExperience must be between 0 and 80.");
        }
    }

    private static string NormalizeProficiency(string proficiency)
    {
        if (!IsValidProficiency(proficiency))
        {
            throw new ArgumentException("Proficiency must be Native, Fluent, Conversational, or Basic.", nameof(proficiency));
        }

        var trimmed = proficiency.Trim();
        return AllowedProficiencies.First(allowed => allowed.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void EnsureActive()
    {
        if (IsDeleted || !IsActive)
        {
            throw new InvalidOperationException("TourGuide.Inactive: inactive guides cannot be modified.");
        }
    }
}
