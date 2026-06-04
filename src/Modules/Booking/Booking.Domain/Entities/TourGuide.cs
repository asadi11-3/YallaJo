using YallaJo.SharedKernel.Domain.Entities;

namespace Booking.Domain.Entities;

public sealed class TourGuide : AuditableEntity, IAggregateRoot
{
    private readonly List<TourGuideLanguage> _tourGuideLanguages = [];
    private readonly List<TourGuideSpecialization> _tourGuideSpecializations = [];
    private readonly List<AvailabilitySlot> _availabilitySlots = [];
    private readonly List<ProviderDocument> _providerDocuments = [];

    private TourGuide() { } // EF Core

    /// <summary>
    /// Provisions a minimal active Booking-side guide registry row for a user.
    /// Used when a guide/provider is activated upstream so that the user can own
    /// availability slots. Profile fields are filled later by other flows.
    /// </summary>
    public static TourGuide Create(Guid userId, bool isActive = true)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        return new TourGuide
        {
            UserId = userId,
            IsActive = isActive,
        };
    }

    /// <summary>Reactivates a previously deactivated guide registry row (idempotent).</summary>
    public void Activate() => IsActive = true;

    public Guid UserId { get; private set; }
    public string? Bio { get; private set; }
    public int YearsOfExperience { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public int CompletedTourCount { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal? HourlyRate { get; private set; }
    public string? Currency { get; private set; }
    public int? ResponseTimeMinutes { get; private set; }

    public IReadOnlyCollection<TourGuideLanguage> TourGuideLanguages => _tourGuideLanguages.AsReadOnly();
    public IReadOnlyCollection<TourGuideSpecialization> TourGuideSpecializations => _tourGuideSpecializations.AsReadOnly();
    public IReadOnlyCollection<AvailabilitySlot> AvailabilitySlots => _availabilitySlots.AsReadOnly();
    public IReadOnlyCollection<ProviderDocument> ProviderDocuments => _providerDocuments.AsReadOnly();
}
