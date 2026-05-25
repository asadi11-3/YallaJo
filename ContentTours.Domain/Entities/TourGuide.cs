using ContentTours.Domain.Enums;
using ContentTours.Domain.Events;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
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

    // Core identity
    public Guid UserId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string? CoverImageUrl { get; private set; }

    // Cross-module links
    public Guid? ApplicationId { get; private set; }
    public Guid? LinkedProviderId { get; private set; }

    // Profile
    public string Bio { get; private set; } = string.Empty;
    public int YearsOfExperience { get; private set; }
    public bool HasFirstAid { get; private set; }
    public string? MoTALicenseNumber { get; private set; }

    // Trust
    public GuideTrustTier TrustTier { get; private set; } = GuideTrustTier.New;

    // Status
    public TourGuideStatus Status { get; private set; } = TourGuideStatus.Active;
    public string? SuspensionReason { get; private set; }
    public Guid? SuspendedByAdminId { get; private set; }
    public DateTime? SuspendedAt { get; private set; }

    // Stats
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public int CompletedTourCount { get; private set; }
    public int ReportCount { get; private set; }

    public IReadOnlyCollection<TourGuideLanguage> Languages => _languages.AsReadOnly();
    public IReadOnlyCollection<TourGuideSpecialization> Specializations => _specializations.AsReadOnly();

    public static Result<TourGuide> Register(
        Guid userId,
        string displayName,
        string slug,
        string bio,
        int yearsOfExperience,
        bool hasFirstAid,
        string? moTALicenseNumber,
        Guid? applicationId = null)
    {
        if (userId == Guid.Empty)
            return Result.Failure<TourGuide>(new Error("TourGuide.InvalidUserId", "UserId is required."));

        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 100)
            return Result.Failure<TourGuide>(new Error("TourGuide.InvalidDisplayName", "DisplayName is required and cannot exceed 100 characters."));

        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > 100)
            return Result.Failure<TourGuide>(new Error("TourGuide.InvalidSlug", "Slug is required and cannot exceed 100 characters."));

        var profileError = ValidateProfile(bio, yearsOfExperience);
        if (profileError is not null)
            return Result.Failure<TourGuide>(profileError);

        var guide = new TourGuide
        {
            UserId = userId,
            Slug = slug.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Bio = bio.Trim(),
            YearsOfExperience = yearsOfExperience,
            HasFirstAid = hasFirstAid,
            MoTALicenseNumber = NormalizeOptional(moTALicenseNumber),
            ApplicationId = applicationId,
            Status = TourGuideStatus.Active,
            TrustTier = GuideTrustTier.New
        };

        guide.AddDomainEvent(new TourGuideRegisteredDomainEvent(guide.Id, guide.UserId));

        return Result.Success(guide);
    }

    public Result UpdateProfile(
        string bio,
        int yearsOfExperience,
        bool hasFirstAid,
        string? moTALicenseNumber)
    {
        var activeError = EnsureActive();
        if (activeError is not null)
            return Result.Failure(activeError);

        var profileError = ValidateProfile(bio, yearsOfExperience);
        if (profileError is not null)
            return Result.Failure(profileError);

        Bio = bio.Trim();
        YearsOfExperience = yearsOfExperience;
        HasFirstAid = hasFirstAid;
        MoTALicenseNumber = NormalizeOptional(moTALicenseNumber);
        MarkUpdated();

        AddDomainEvent(new TourGuideUpdatedDomainEvent(Id, UserId));
        return Result.Success();
    }

    public Result UpdateAvatar(string avatarUrl)
    {
        var activeError = EnsureActive();
        if (activeError is not null)
            return Result.Failure(activeError);

        if (string.IsNullOrWhiteSpace(avatarUrl))
            return Result.Failure(new Error("TourGuide.InvalidAvatarUrl", "AvatarUrl is required."));

        AvatarUrl = avatarUrl.Trim();
        MarkUpdated();
        return Result.Success();
    }

    public Result UpdateCoverImage(string? coverImageUrl)
    {
        var activeError = EnsureActive();
        if (activeError is not null)
            return Result.Failure(activeError);

        CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim();
        MarkUpdated();
        return Result.Success();
    }

    public Result ChangeSlug(string newSlug)
    {
        var activeError = EnsureActive();
        if (activeError is not null)
            return Result.Failure(activeError);

        if (string.IsNullOrWhiteSpace(newSlug) || newSlug.Trim().Length > 100)
            return Result.Failure(new Error("TourGuide.InvalidSlug", "Slug is required and cannot exceed 100 characters."));

        Slug = newSlug.Trim().ToLowerInvariant();
        MarkUpdated();
        return Result.Success();
    }

    public Result Suspend(Guid adminId, string reason, DateTime utcNow)
    {
        if (Status == TourGuideStatus.Deactivated)
            return Result.Failure(new Error("TourGuide.Deactivated", "Deactivated guides cannot be suspended."));

        if (Status == TourGuideStatus.Suspended)
            return Result.Failure(new Error("TourGuide.AlreadySuspended", "The guide is already suspended."));

        Status = TourGuideStatus.Suspended;
        SuspensionReason = reason.Trim();
        SuspendedByAdminId = adminId;
        SuspendedAt = utcNow;
        MarkUpdated();
        return Result.Success();
    }

    public Result Reinstate(Guid adminId)
    {
        if (Status != TourGuideStatus.Suspended)
            return Result.Failure(new Error("TourGuide.NotSuspended", "Only suspended guides can be reinstated."));

        Status = TourGuideStatus.Active;
        SuspensionReason = null;
        SuspendedByAdminId = null;
        SuspendedAt = null;
        MarkUpdated();
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (Status == TourGuideStatus.Deactivated)
            return Result.Failure(new Error("TourGuide.AlreadyDeactivated", "The guide is already deactivated."));

        Status = TourGuideStatus.Deactivated;
        SoftDelete();
        return Result.Success();
    }

    public Result PromoteTier(Guid adminId, GuideTrustTier targetTier)
    {
        if (targetTier <= TrustTier)
            return Result.Failure(new Error("TourGuide.InvalidTierPromotion", "Target tier must be higher than current tier."));

        TrustTier = targetTier;
        MarkUpdated();
        return Result.Success();
    }

    public Result DemoteTier(GuideTrustTier targetTier)
    {
        if (targetTier >= TrustTier)
            return Result.Failure(new Error("TourGuide.InvalidTierDemotion", "Target tier must be lower than current tier."));

        TrustTier = targetTier;
        MarkUpdated();
        return Result.Success();
    }

    public Result LinkProvider(Guid providerId)
    {
        LinkedProviderId = providerId;
        MarkUpdated();
        return Result.Success();
    }

    public void IncrementCompletedTourCount()
    {
        CompletedTourCount++;
        MarkUpdated();
    }

    public void IncrementReportCount()
    {
        ReportCount++;
        MarkUpdated();
    }

    public Result AddLanguage(Guid languageId, string proficiency)
    {
        var activeError = EnsureActive();
        if (activeError is not null)
            return Result.Failure(activeError);

        if (languageId == Guid.Empty)
            return Result.Failure(new Error("TourGuide.InvalidLanguageId", "LanguageId is required."));

        var proficiencyResult = NormalizeProficiency(proficiency);
        if (proficiencyResult.IsFailure)
            return Result.Failure(proficiencyResult.Error);

        if (_languages.Any(language => language.LanguageId == languageId))
            return Result.Failure(new Error("TourGuideLanguage.AlreadyExists", "This language is already assigned to the guide."));

        _languages.Add(TourGuideLanguage.Create(Id, languageId, proficiencyResult.Value));
        MarkUpdated();

        AddDomainEvent(new TourGuideLanguageAddedDomainEvent(Id, UserId, languageId, proficiencyResult.Value));
        return Result.Success();
    }

    public Result RemoveLanguage(Guid languageId)
    {
        var activeError = EnsureActive();
        if (activeError is not null)
            return Result.Failure(activeError);

        var language = _languages.FirstOrDefault(item => item.LanguageId == languageId);
        if (language is null)
            return Result.Failure(new Error("TourGuideLanguage.NotFound", "This language is not assigned to the guide."));

        if (_languages.Count == 1)
            return Result.Failure(new Error("TourGuideLanguage.LastLanguage", "A guide must keep at least one language."));

        _languages.Remove(language);
        MarkUpdated();

        AddDomainEvent(new TourGuideLanguageRemovedDomainEvent(Id, UserId, languageId));
        return Result.Success();
    }

    public Result AddSpecialization(Guid specializationId)
    {
        var activeError = EnsureActive();
        if (activeError is not null)
            return Result.Failure(activeError);

        if (specializationId == Guid.Empty)
            return Result.Failure(new Error("TourGuide.InvalidSpecializationId", "SpecializationId is required."));

        if (_specializations.Any(specialization => specialization.SpecializationId == specializationId))
            return Result.Failure(new Error("TourGuideSpecialization.AlreadyExists", "This specialization is already assigned to the guide."));

        _specializations.Add(TourGuideSpecialization.Create(Id, specializationId));
        MarkUpdated();

        AddDomainEvent(new TourGuideSpecializationAddedDomainEvent(Id, UserId, specializationId));
        return Result.Success();
    }

    public Result RemoveSpecialization(Guid specializationId)
    {
        var activeError = EnsureActive();
        if (activeError is not null)
            return Result.Failure(activeError);

        var specialization = _specializations.FirstOrDefault(item => item.SpecializationId == specializationId);
        if (specialization is null)
            return Result.Failure(new Error("TourGuideSpecialization.NotFound", "This specialization is not assigned to the guide."));

        _specializations.Remove(specialization);
        MarkUpdated();
        return Result.Success();
    }

    public static bool IsValidProficiency(string proficiency) =>
        !string.IsNullOrWhiteSpace(proficiency) && AllowedProficiencies.Contains(proficiency.Trim());

    private static Error? ValidateProfile(string bio, int yearsOfExperience)
    {
        if (string.IsNullOrWhiteSpace(bio))
            return new Error("TourGuide.BioRequired", "Bio is required.");

        if (bio.Trim().Length > 2000)
            return new Error("TourGuide.BioTooLong", "Bio cannot exceed 2000 characters.");

        if (yearsOfExperience is < 0 or > 80)
            return new Error("TourGuide.InvalidYearsOfExperience", "YearsOfExperience must be between 0 and 80.");

        return null;
    }

    private static Result<string> NormalizeProficiency(string proficiency)
    {
        if (!IsValidProficiency(proficiency))
            return Result.Failure<string>(new Error("TourGuide.InvalidProficiency", "Proficiency must be Native, Fluent, Conversational, or Basic."));

        var trimmed = proficiency.Trim();
        return Result.Success<string>(AllowedProficiencies.First(allowed => allowed.Equals(trimmed, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Error? EnsureActive()
    {
        if (Status != TourGuideStatus.Active || IsDeleted)
            return new Error("TourGuide.NotActive", "Only active guides can perform this action.");
        return null;
    }
}
