using ContentTours.Domain.Enums;
using ContentTours.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Domain.Entities;

public sealed class Tour : AuditableEntity, IAggregateRoot
{
    private readonly List<TourTranslation> _tourTranslations = [];
    private readonly List<TourSchedule> _tourSchedules = [];
    private readonly List<TourWaypoint> _tourWaypoints = [];
    private readonly List<TourPricingTier> _tourPricingTiers = [];
    private readonly List<TourPackage> _tourPackages = [];

    private Tour() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ShortDescription { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public int DurationMinutes { get; private set; }
    public int MaxGroupSize { get; private set; }
    public int? MinAge { get; private set; }
    public Money BasePrice { get; private set; } = default!;
    public string Currency { get; private set; } = string.Empty;
    public Location Location { get; private set; } = default!;
    public Location? MeetingPoint { get; private set; }
    public TourStatus Status { get; private set; } = TourStatus.Draft;
    public decimal AverageRating { get; private set; } = 0m;
    public int ReviewCount { get; private set; }
    public int BookingCount { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsInstantBooking { get; private set; }
    public int CancellationPolicyHours { get; private set; } = 24;
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? PlaceId { get; private set; }
    public bool IsChildFriendly { get; private set; }
    public bool IsAccessible { get; private set; }
    public int? AgeRestriction { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public decimal? SalePrice { get; private set; }
    public string? SalePriceCurrency { get; private set; }
    public DateTime? DiscountValidFrom { get; private set; }
    public DateTime? DiscountValidTo { get; private set; }

    public IReadOnlyCollection<TourTranslation> TourTranslations => _tourTranslations.AsReadOnly();
    public IReadOnlyCollection<TourSchedule> TourSchedules => _tourSchedules.AsReadOnly();
    public IReadOnlyCollection<TourWaypoint> TourWaypoints => _tourWaypoints.AsReadOnly();
    public IReadOnlyCollection<TourPricingTier> TourPricingTiers => _tourPricingTiers.AsReadOnly();
    public IReadOnlyCollection<TourPackage> TourPackages => _tourPackages.AsReadOnly();

    // ── Business Methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Transitions this tour from Draft to Published.
    /// Raises <see cref="TourPlaceCountChangedDomainEvent"/> so the linked Place's
    /// TourCount can be incremented via the ContentPlaces outbox.
    /// </summary>
    public void Publish()
    {
        if (Status == TourStatus.Published)
            return; // already published — guard to avoid spurious domain events

        Status = TourStatus.Published;
        MarkUpdated();

        // Notify ContentPlaces to recompute its denormalized TourCount.
        if (PlaceId.HasValue)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    /// <summary>
    /// Archives (deactivates) this tour.
    /// Raises <see cref="TourPlaceCountChangedDomainEvent"/> so the linked Place's
    /// TourCount can be decremented via the ContentPlaces outbox.
    /// </summary>
    public void Archive()
    {
        if (Status == TourStatus.Archived)
            return;

        Status = TourStatus.Archived;
        MarkUpdated();

        if (PlaceId.HasValue)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    /// <summary>Suspends this tour. Also affects the linked Place's TourCount.</summary>
    public void Suspend()
    {
        if (Status == TourStatus.Suspended)
            return;

        Status = TourStatus.Suspended;
        MarkUpdated();

        if (PlaceId.HasValue)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    /// <summary>
    /// Links this tour to a place (or moves it to a different place).
    /// Raises a <see cref="TourPlaceCountChangedDomainEvent"/> for both the old and
    /// the new PlaceId so both places get their counts recomputed.
    /// </summary>
    public void AssignToPlace(Guid placeId)
    {
        if (placeId == Guid.Empty)
            throw new ArgumentException("PlaceId cannot be empty.", nameof(placeId));

        if (PlaceId == placeId)
            return; // no change

        var oldPlaceId = PlaceId;
        PlaceId = placeId;
        MarkUpdated();

        // Notify old place to decrement (if there was one)
        if (oldPlaceId.HasValue)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, oldPlaceId.Value));

        // Notify new place to increment (only relevant when tour is Published)
        if (Status == TourStatus.Published)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, placeId));
    }

    /// <summary>Removes the place association from this tour.</summary>
    public void RemoveFromPlace()
    {
        if (!PlaceId.HasValue)
            return;

        var oldPlaceId = PlaceId.Value;
        PlaceId = null;
        MarkUpdated();

        if (Status == TourStatus.Published)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, oldPlaceId));
    }

    /// <summary>
    /// Soft-deletes this tour and notifies the linked Place to recompute TourCount.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
            return;

        SoftDelete();

        if (PlaceId.HasValue && Status == TourStatus.Published)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }
}
