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
    /// PW-1 lifecycle: transitions a Draft (or Rejected) tour to Pending so the admin can review.
    /// Enforces submit-time invariants (must have at least one active schedule and one active
    /// Adult pricing tier) so an incomplete tour can never enter the review queue.
    ///
    /// The collections are passed in (rather than read off navigation properties) so the caller
    /// is forced to load them via the dedicated repositories — the Tour aggregate's children are
    /// not eagerly loaded by default.
    /// </summary>
    /// <param name="hasActiveSchedule">True iff at least one <c>TourSchedule</c> with <c>IsActive == true</c> exists for this tour.</param>
    /// <param name="hasActiveAdultTier">True iff at least one <c>TourPricingTier</c> with <c>IsActive &amp;&amp; ParticipantType == Adult</c> exists.</param>
    /// <exception cref="InvalidOperationException">Thrown when status is not submittable, or invariants fail.</exception>
    public void Submit(bool hasActiveSchedule, bool hasActiveAdultTier)
    {
        if (!Status.IsSubmittable())
            throw new InvalidOperationException(
                $"Tour.InvalidStateForSubmit: status {Status} cannot be submitted. " +
                $"Required: Draft or Rejected.");

        if (!hasActiveSchedule)
            throw new InvalidOperationException(
                "Tour.NoActiveSchedule: at least one active TourSchedule is required to submit.");

        if (!hasActiveAdultTier)
            throw new InvalidOperationException(
                "Tour.NoAdultPricingTier: at least one active Adult TourPricingTier is required to submit.");

        Status = TourStatus.Pending;
        MarkUpdated();
    }

    /// <summary>
    /// PW-1 lifecycle: admin approves a Pending tour, making it publicly visible.
    /// Also fires when a Suspended/Archived tour is reinstated.
    /// Raises <see cref="TourPlaceCountChangedDomainEvent"/> so the linked Place's TourCount can
    /// be incremented via the ContentPlaces outbox.
    /// </summary>
    public void Approve()
    {
        if (Status == TourStatus.Approved)
            return; // already approved — guard to avoid spurious domain events

        Status = TourStatus.Approved;
        MarkUpdated();

        // Notify ContentPlaces to recompute its denormalized TourCount.
        if (PlaceId.HasValue)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    /// <summary>
    /// PW-1 lifecycle: admin rejects a Pending tour. Provider may amend and call <see cref="Submit"/> again.
    /// </summary>
    public void Reject()
    {
        if (Status == TourStatus.Rejected)
            return;

        Status = TourStatus.Rejected;
        MarkUpdated();
    }

    /// <summary>
    /// Updates the tour's pricing currency. Blocked when any <c>TourPricingTier</c> exists,
    /// because changing currency would silently invalidate every tier's price/currency match.
    /// The caller is responsible for passing the actual tier count (the aggregate does not load
    /// tiers eagerly).
    ///
    /// Set <c>FIX-12</c> in <c>MOHAMMAD_TASK2_3_FIX_PLAN.md</c>.
    /// </summary>
    /// <param name="newCurrency">3-letter ISO currency code (e.g. "JOD"). Case-insensitive.</param>
    /// <param name="existingTierCount">The number of <c>TourPricingTier</c> rows currently attached to this tour.</param>
    /// <exception cref="ArgumentException">Thrown when the currency code is empty or not 3 characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when tiers exist and the currency would change.</exception>
    public void ChangeCurrency(string newCurrency, int existingTierCount)
    {
        if (string.IsNullOrWhiteSpace(newCurrency))
            throw new ArgumentException("Currency is required.", nameof(newCurrency));
        if (newCurrency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(newCurrency));

        var normalized = newCurrency.ToUpperInvariant();
        if (Currency.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            return; // no-op — caller passed the same currency

        if (existingTierCount > 0)
            throw new InvalidOperationException(
                "Tour.CurrencyLockedByPricingTiers: cannot change currency while pricing tiers exist. " +
                "Delete or update all tiers to the new currency first.");

        Currency = normalized;
        MarkUpdated();
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

        // Notify new place to increment (only relevant when tour is Approved)
        if (Status == TourStatus.Approved)
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

        if (Status == TourStatus.Approved)
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

        if (PlaceId.HasValue && Status == TourStatus.Approved)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    /// <summary>
    /// Toggles the IsFeatured flag. Idempotent — raises no event if value unchanged (ERR-009).
    /// Only raises <see cref="TourFeaturedChangedDomainEvent"/> when the value actually changes.
    /// </summary>
    public void SetFeatured(bool isFeatured, Guid changedByUserId)
    {
        if (IsFeatured == isFeatured)
            return; // ERR-009 idempotency: no event when value unchanged

        IsFeatured = isFeatured;
        MarkUpdated();

        AddDomainEvent(new TourFeaturedChangedDomainEvent(
            TourId: Id,
            IsFeatured: isFeatured,
            ChangedByUserId: changedByUserId,
            ChangedAt: DateTime.UtcNow));
    }
}
