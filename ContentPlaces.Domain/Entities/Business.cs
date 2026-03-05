using ContentPlaces.Domain.Enums;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentPlaces.Domain.Entities;

public sealed class Business : AuditableEntity, IAggregateRoot
{
    private readonly List<BusinessTranslation> _businessTranslations = [];
    private readonly List<BusinessHours> _businessHours = [];
    private readonly List<ServiceItem> _serviceItems = [];
    private readonly List<BusinessStaff> _staff = [];
    private readonly List<BusinessAmenity> _amenities = [];

    private Business() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public BusinessType BusinessType { get; private set; }
    public Guid? PlaceId { get; private set; }
    public Location Location { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsFeatured { get; private set; }
    public Guid OwnerId { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? LicenseNumber { get; private set; }
    public string? TaxId { get; private set; }
    public BusinessStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public SubscriptionTier? SubscriptionTier { get; private set; }

    public IReadOnlyCollection<BusinessTranslation> BusinessTranslations => _businessTranslations.AsReadOnly();
    public IReadOnlyCollection<BusinessHours> BusinessHours => _businessHours.AsReadOnly();
    public IReadOnlyCollection<ServiceItem> ServiceItems => _serviceItems.AsReadOnly();
    public IReadOnlyCollection<BusinessStaff> Staff => _staff.AsReadOnly();
    public IReadOnlyCollection<BusinessAmenity> Amenities => _amenities.AsReadOnly();
}
