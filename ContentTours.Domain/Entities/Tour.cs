using ContentTours.Domain.Enums;
using ContentTours.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Domain.Entities;

public sealed class Tour : AuditableEntity, IAggregateRoot
{
    private static readonly HashSet<string> AllowedCurrencies =
        new(StringComparer.OrdinalIgnoreCase) { "JOD", "USD", "EUR" };

    private readonly List<TourTranslation> _tourTranslations = [];
    private readonly List<TourSchedule> _tourSchedules = [];
    private readonly List<TourWaypoint> _tourWaypoints = [];
    private readonly List<TourPricingTier> _tourPricingTiers = [];
    private readonly List<TourPackage> _tourPackages = [];
    private readonly List<TourChildFacility> _childFacilities = [];

    private Tour()
    {
    }

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
    public bool AllowsChildren { get; private set; }
    public int? MinChildAge { get; private set; }
    public int? MaxChildAge { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public decimal? SalePrice { get; private set; }
    public string? SalePriceCurrency { get; private set; }
    public DateTime? DiscountValidFrom { get; private set; }
    public DateTime? DiscountValidTo { get; private set; }

    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public Guid? RejectedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTime? ReinstatedAt { get; private set; }

    public IReadOnlyCollection<TourTranslation> TourTranslations => _tourTranslations.AsReadOnly();
    public IReadOnlyCollection<TourSchedule> TourSchedules => _tourSchedules.AsReadOnly();
    public IReadOnlyCollection<TourWaypoint> TourWaypoints => _tourWaypoints.AsReadOnly();
    public IReadOnlyCollection<TourPricingTier> TourPricingTiers => _tourPricingTiers.AsReadOnly();
    public IReadOnlyCollection<TourPackage> TourPackages => _tourPackages.AsReadOnly();
    public IReadOnlyList<TourChildFacility> ChildFacilities => _childFacilities.AsReadOnly();

    public static Tour Create(
        string name,
        string slug,
        Difficulty difficulty,
        int durationMinutes,
        int maxGroupSize,
        decimal basePriceAmount,
        string currency,
        Location location,
        Guid createdByUserId,
        string? description = null,
        string? shortDescription = null,
        int? minAge = null,
        Location? meetingPoint = null,
        Guid? placeId = null,
        bool isChildFriendly = false,
        bool isAccessible = false,
        int? ageRestriction = null,
        bool isInstantBooking = false,
        int cancellationPolicyHours = 24,
        string? metaTitle = null,
        string? metaDescription = null)
    {
        ValidateName(name);
        ValidateSlug(slug);
        ValidateDuration(durationMinutes);
        ValidateMaxGroupSize(maxGroupSize);
        ValidateBasePrice(basePriceAmount);
        var normalizedCurrency = ValidateAndNormalizeCurrency(currency);
        ValidateLocation(location);
        ValidatePlaceId(placeId);
        ValidateCancellationPolicyHours(cancellationPolicyHours);
        ValidateAge(minAge, nameof(minAge));
        ValidateAge(ageRestriction, nameof(ageRestriction));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));

        var tour = new Tour
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            ShortDescription = shortDescription?.Trim(),
            Difficulty = difficulty,
            DurationMinutes = durationMinutes,
            MaxGroupSize = maxGroupSize,
            MinAge = minAge,
            BasePrice = new Money(basePriceAmount, normalizedCurrency),
            Currency = normalizedCurrency,
            Location = location,
            MeetingPoint = meetingPoint,
            Status = TourStatus.Draft,
            CreatedByUserId = createdByUserId,
            PlaceId = placeId,
            IsChildFriendly = isChildFriendly,
            IsAccessible = isAccessible,
            AgeRestriction = ageRestriction,
            IsInstantBooking = isInstantBooking,
            CancellationPolicyHours = cancellationPolicyHours,
            MetaTitle = metaTitle?.Trim(),
            MetaDescription = metaDescription?.Trim(),
        };

        tour.AddDomainEvent(new TourCreatedDomainEvent(
            TourId: tour.Id,
            Name: tour.Name,
            Description: tour.Description,
            ShortDescription: tour.ShortDescription,
            MetaTitle: tour.MetaTitle,
            MetaDescription: tour.MetaDescription,
            Slug: tour.Slug,
            CreatedByUserId: tour.CreatedByUserId,
            PlaceId: tour.PlaceId));

        return tour;
    }

    public void Update(
        string name,
        string slug,
        Difficulty difficulty,
        int durationMinutes,
        int maxGroupSize,
        decimal basePriceAmount,
        string currency,
        Location location,
        string? description = null,
        string? shortDescription = null,
        int? minAge = null,
        Location? meetingPoint = null,
        Guid? placeId = null,
        bool isChildFriendly = false,
        bool isAccessible = false,
        int? ageRestriction = null,
        bool isInstantBooking = false,
        int cancellationPolicyHours = 24,
        string? metaTitle = null,
        string? metaDescription = null)
    {
        EnsureNotDeleted();
        EnsureMutable();

        ValidateName(name);
        ValidateSlug(slug);
        ValidateDuration(durationMinutes);
        ValidateMaxGroupSize(maxGroupSize);
        ValidateBasePrice(basePriceAmount);
        var normalizedCurrency = ValidateAndNormalizeCurrency(currency);
        ValidateLocation(location);
        ValidatePlaceId(placeId);
        ValidateCancellationPolicyHours(cancellationPolicyHours);
        ValidateAge(minAge, nameof(minAge));
        ValidateAge(ageRestriction, nameof(ageRestriction));

        var newName = name.Trim();
        var newSlug = slug.Trim().ToLowerInvariant();
        var newDescription = description?.Trim();
        var newShortDescription = shortDescription?.Trim();

        var nameChanged = !string.Equals(Name, newName, StringComparison.Ordinal);
        var descriptionChanged = !string.Equals(Description, newDescription, StringComparison.Ordinal);
        var shortDescriptionChanged = !string.Equals(ShortDescription, newShortDescription, StringComparison.Ordinal);
        var placeIdChanged = PlaceId != placeId;

        Name = newName;
        Slug = newSlug;
        Description = newDescription;
        ShortDescription = newShortDescription;
        Difficulty = difficulty;
        DurationMinutes = durationMinutes;
        MaxGroupSize = maxGroupSize;
        MinAge = minAge;
        BasePrice = new Money(basePriceAmount, normalizedCurrency);
        Currency = normalizedCurrency;
        Location = location;
        MeetingPoint = meetingPoint;
        PlaceId = placeId;
        IsChildFriendly = isChildFriendly;
        IsAccessible = isAccessible;
        AgeRestriction = ageRestriction;
        IsInstantBooking = isInstantBooking;
        CancellationPolicyHours = cancellationPolicyHours;
        MetaTitle = metaTitle?.Trim();
        MetaDescription = metaDescription?.Trim();

        if (Status == TourStatus.Rejected)
        {
            Status = TourStatus.Draft;
            RejectedAt = null;
            RejectedByUserId = null;
            RejectionReason = null;
        }

        MarkUpdated();

        AddDomainEvent(new TourUpdatedDomainEvent(
            TourId: Id,
            NameChanged: nameChanged,
            DescriptionChanged: descriptionChanged,
            ShortDescriptionChanged: shortDescriptionChanged,
            PlaceIdChanged: placeIdChanged,
            ChildrenInfoChanged: false));
    }

    public void Submit()
    {
        EnsureNotDeleted();
        if (Status != TourStatus.Draft)
        {
            throw new InvalidOperationException(
               $"Tour.InvalidTransition: cannot submit a tour with status {Status}. Required: Draft.");
        }

        Status = TourStatus.Pending;
        SubmittedAt = DateTime.UtcNow;
        MarkUpdated();

        AddDomainEvent(new TourSubmittedDomainEvent(
            TourId: Id,
            CreatedByUserId: CreatedByUserId,
            SubmittedAt: SubmittedAt.Value));
    }

    public void Approve(Guid reviewerId)
    {
        EnsureNotDeleted();
        if (reviewerId == Guid.Empty)
            throw new ArgumentException("Reviewer id is required.", nameof(reviewerId));
        if (Status != TourStatus.Pending)
        {
            throw new InvalidOperationException(
              $"Tour.InvalidTransition: cannot approve a tour with status {Status}. Required: Pending.");
        }

        Status = TourStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ApprovedByUserId = reviewerId;
        MarkUpdated();

        AddDomainEvent(new TourApprovedDomainEvent(
            TourId: Id,
            CreatedByUserId: CreatedByUserId,
            ApprovedByUserId: reviewerId,
            ApprovedAt: ApprovedAt.Value));

        if (PlaceId.HasValue)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    public void Reject(string reason, Guid reviewerId)
    {
        EnsureNotDeleted();
        if (reviewerId == Guid.Empty)
            throw new ArgumentException("Reviewer id is required.", nameof(reviewerId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));
        if (reason.Length > 1000)
            throw new ArgumentException("Rejection reason cannot exceed 1000 characters.", nameof(reason));
        if (Status != TourStatus.Pending) {
            throw new InvalidOperationException(
               $"Tour.InvalidTransition: cannot reject a tour with status {Status}. Required: Pending.");
        }

        Status = TourStatus.Rejected;
        RejectedAt = DateTime.UtcNow;
        RejectedByUserId = reviewerId;
        RejectionReason = reason.Trim();
        MarkUpdated();

        AddDomainEvent(new TourRejectedDomainEvent(
            TourId: Id,
            CreatedByUserId: CreatedByUserId,
            RejectedByUserId: reviewerId,
            Reason: RejectionReason,
            RejectedAt: RejectedAt.Value));
    }

    public void Suspend(string reason)
    {
        EnsureNotDeleted();
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Suspension reason is required.", nameof(reason));
        if (reason.Length > 1000)
            throw new ArgumentException("Suspension reason cannot exceed 1000 characters.", nameof(reason));
        if (Status != TourStatus.Approved) {
            throw new InvalidOperationException(
               $"Tour.InvalidTransition: cannot suspend a tour with status {Status}. Required: Approved.");
        }

        Status = TourStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
        SuspensionReason = reason.Trim();
        MarkUpdated();

        AddDomainEvent(new TourSuspendedDomainEvent(
            TourId: Id,
            CreatedByUserId: CreatedByUserId,
            Reason: SuspensionReason,
            SuspendedAt: SuspendedAt.Value));

        if (PlaceId.HasValue)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    public void Reinstate()
    {
        EnsureNotDeleted();
        if (Status != TourStatus.Suspended)
        {
            throw new InvalidOperationException(
                $"Tour.InvalidTransition: cannot reinstate a tour with status {Status}. Required: Suspended.");
        }

        Status = TourStatus.Approved;
        ReinstatedAt = DateTime.UtcNow;
        SuspensionReason = null;
        SuspendedAt = null;
        MarkUpdated();

        AddDomainEvent(new TourReinstatedDomainEvent(
            TourId: Id,
            CreatedByUserId: CreatedByUserId,
            ReinstatedAt: ReinstatedAt.Value));

        if (PlaceId.HasValue)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    public new void SoftDelete()
    {
        if (IsDeleted) return;

        base.SoftDelete();

        if (PlaceId.HasValue && Status == TourStatus.Approved)
            AddDomainEvent(new TourPlaceCountChangedDomainEvent(Id, PlaceId.Value));
    }

    public void SetFeatured(bool isFeatured, Guid changedByUserId)
    {
        EnsureNotDeleted();
        if (changedByUserId == Guid.Empty)
            throw new ArgumentException("ChangedByUserId is required.", nameof(changedByUserId));
        if (IsFeatured == isFeatured)
            return;

        IsFeatured = isFeatured;
        MarkUpdated();

        AddDomainEvent(new TourFeaturedChangedDomainEvent(
            TourId: Id,
            IsFeatured: isFeatured,
            ChangedByUserId: changedByUserId,
            ChangedAt: DateTime.UtcNow));
    }

    public void UpdateRating(decimal averageRating, int reviewCount)
    {
        EnsureNotDeleted();
        if (averageRating is < 0m or > 5m)
            throw new ArgumentOutOfRangeException(nameof(averageRating), "Average rating must be between 0 and 5.");
        if (reviewCount < 0)
            throw new ArgumentOutOfRangeException(nameof(reviewCount), "Review count cannot be negative.");

        AverageRating = Math.Round(averageRating, 2);
        ReviewCount = reviewCount;
        MarkUpdated();
    }

    public void UpdateBookingCount(int delta)
    {
        EnsureNotDeleted();
        var next = BookingCount + delta;
        if (next < 0) next = 0;
        BookingCount = next;
        MarkUpdated();
    }

    public void ApplyDiscount(decimal percent, DateTime validFromUtc, DateTime validToUtc)
    {
        EnsureNotDeleted();
        if (BasePrice is null || BasePrice.Amount <= 0m)
        {
            throw new InvalidOperationException(
                "Tour.DiscountIncoherent: cannot apply a discount when BasePrice is zero or unset.");
        }

        if (percent is <= 0m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(percent), "Discount percent must be in (0, 100].");
        if (validFromUtc >= validToUtc)
            throw new ArgumentException("DiscountValidFrom must be earlier than DiscountValidTo.", nameof(validFromUtc));

        var discounted = BasePrice.ApplyDiscount(percent);
        if (discounted.Amount >= BasePrice.Amount)
        {
            throw new InvalidOperationException(
                "Tour.DiscountIncoherent: computed SalePrice must be strictly less than BasePrice.");
        }

        DiscountPercent = Math.Round(percent, 2);
        DiscountValidFrom = validFromUtc;
        DiscountValidTo = validToUtc;
        SalePrice = discounted.Amount;
        SalePriceCurrency = discounted.Currency;
        MarkUpdated();
    }

    public void RemoveDiscount()
    {
        EnsureNotDeleted();
        if (DiscountPercent is null && SalePrice is null && DiscountValidFrom is null && DiscountValidTo is null)
            return;

        DiscountPercent = null;
        SalePrice = null;
        SalePriceCurrency = null;
        DiscountValidFrom = null;
        DiscountValidTo = null;
        MarkUpdated();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Tour.Deleted: operation not permitted on a soft-deleted tour.");
    }

    private void EnsureMutable()
    {
        if (!Status.IsEditable())
        {
            throw new InvalidOperationException(
               $"Tour.InvalidTransition: tour cannot be edited in status {Status}. Required: Draft or Rejected.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (name.Trim().Length > 300)
            throw new ArgumentException("Name cannot exceed 300 characters.", nameof(name));
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required.", nameof(slug));
        var trimmed = slug.Trim();
        if (trimmed.Length > 300)
            throw new ArgumentException("Slug cannot exceed 300 characters.", nameof(slug));
    }

    private static void ValidateDuration(int durationMinutes)
    {
        if (durationMinutes <= 0 || durationMinutes > 43200)
        {
            throw new ArgumentOutOfRangeException(
               nameof(durationMinutes),
               "DurationMinutes must be between 1 and 43200 (30 days).");
        }
    }

    private static void ValidateMaxGroupSize(int maxGroupSize)
    {
        if (maxGroupSize <= 0 || maxGroupSize > 500)
        {
            throw new ArgumentOutOfRangeException(
              nameof(maxGroupSize),
              "MaxGroupSize must be between 1 and 500.");
        }
    }

    private static void ValidateBasePrice(decimal basePriceAmount)
    {
        if (basePriceAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
               nameof(basePriceAmount),
               "BasePrice cannot be negative.");
        }
    }

    private static string ValidateAndNormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));
        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        var upper = currency.ToUpperInvariant();
        if (!AllowedCurrencies.Contains(upper))
        {
            throw new ArgumentException(
                $"Currency '{currency}' is not supported. Allowed: JOD, USD, EUR.",
                nameof(currency));
        }

        return upper;
    }

    private static void ValidateLocation(Location location)
    {
        if (location is null)
            throw new ArgumentNullException(nameof(location), "Location is required.");
    }

    private static void ValidatePlaceId(Guid? placeId)
    {
        if (placeId.HasValue && placeId.Value == Guid.Empty)
            throw new ArgumentException("PlaceId cannot be Guid.Empty.", nameof(placeId));
    }

    private static void ValidateCancellationPolicyHours(int hours)
    {
        if (hours < 0 || hours > 168)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hours),
                "CancellationPolicyHours must be between 0 and 168.");
        }
    }

    private static void ValidateAge(int? age, string paramName)
    {
        if (age.HasValue && (age.Value < 0 || age.Value > 120))
            throw new ArgumentOutOfRangeException(paramName, "Age must be between 0 and 120.");
    }

    private static void ValidateChildAge(int? age, string paramName)
    {
        if (age.HasValue && (age.Value < 0 || age.Value > 18))
            throw new ArgumentOutOfRangeException(paramName, "Child age must be between 0 and 18.");
    }

    public void UpdateChildrenInfo(
        bool allowsChildren,
        int? minChildAge,
        int? maxChildAge,
        IReadOnlyList<ChildFacility>? childFacilities)
    {
        EnsureNotDeleted();
        EnsureMutable();

        if (allowsChildren)
        {
            ValidateChildAge(minChildAge, nameof(minChildAge));
            ValidateChildAge(maxChildAge, nameof(maxChildAge));

            if (minChildAge.HasValue && maxChildAge.HasValue && maxChildAge.Value < minChildAge.Value)
            {
                throw new ArgumentException(
                    "Tour.ChildrenInfoInvalid: MaxChildAge must be greater than or equal to MinChildAge.",
                    nameof(maxChildAge));
            }
        }

        AllowsChildren = allowsChildren;

        IsChildFriendly = allowsChildren;

        if (allowsChildren)
        {
            MinChildAge = minChildAge;
            MaxChildAge = maxChildAge;
        }
        else
        {
            MinChildAge = null;
            MaxChildAge = null;
        }

        _childFacilities.Clear();

        if (allowsChildren && childFacilities is not null && childFacilities.Count > 0)
        {
            foreach (var facility in childFacilities.Distinct().OrderBy(f => f))
            {
                _childFacilities.Add(new TourChildFacility(Id, facility));
            }
        }

        MarkUpdated();

        AddDomainEvent(new TourUpdatedDomainEvent(
            TourId: Id,
            NameChanged: false,
            DescriptionChanged: false,
            ShortDescriptionChanged: false,
            PlaceIdChanged: false,
            ChildrenInfoChanged: true));
    }
}
