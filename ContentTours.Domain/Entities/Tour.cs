using ContentTours.Domain.Enums;
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
}
