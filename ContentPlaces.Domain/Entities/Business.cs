using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Events.BusinessEvents;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentPlaces.Domain.Entities;

public sealed class Business : AuditableEntity, IAggregateRoot
{
    private static readonly Regex SlugRegex = new(
        @"[^a-z0-9\-]",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private readonly List<BusinessTranslation> _businessTranslations = [];
    private readonly List<BusinessHours> _businessHours = [];
    private readonly List<ServiceItem> _serviceItems = [];
    private readonly List<BusinessStaff> _staff = [];
    private readonly List<BusinessAmenity> _amenities = [];

    private Business()
    {
    } // EF Core

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

    public static Business Create(
        string name,
        string slug,
        BusinessType businessType,
        Guid ownerId,
        Location location,
        Guid? placeId = null,
        string? description = null,
        string? address = null,
        string? city = null,
        string? country = null,
        string? postalCode = null,
        string? phone = null,
        string? email = null,
        string? website = null,
        string? licenseNumber = null,
        string? taxId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Business name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Business slug is required.", nameof(slug));

        if (ownerId == Guid.Empty)
            throw new ArgumentException("OwnerId cannot be empty.", nameof(ownerId));

        ArgumentNullException.ThrowIfNull(location);

        var business = new Business
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Slug = slug.Trim(),
            BusinessType = businessType,
            OwnerId = ownerId,
            Location = location,
            PlaceId = placeId,
            Description = description?.Trim(),
            Address = address?.Trim(),
            City = city?.Trim(),
            Country = country?.Trim(),
            PostalCode = postalCode?.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Website = website?.Trim(),
            LicenseNumber = licenseNumber?.Trim(),
            TaxId = taxId?.Trim(),
            Status = BusinessStatus.Pending,
            AverageRating = 0,
            ReviewCount = 0,
            IsVerified = false,
            IsFeatured = false
        };

        for (int i = 0; i < 7; i++)
        {
            business._businessHours.Add(Entities.BusinessHours.Create(
                businessId: business.Id,
                dayOfWeek: (ContentPlaces.Domain.Enums.DayOfWeek)i,
                openTime: TimeOnly.MinValue,
                closeTime: TimeOnly.MinValue,
                isClosed: true));
        }

        business.AddDomainEvent(new BusinessCreatedDomainEvent(
                 business.Id, business.Name, business.Slug, business.OwnerId, business.PlaceId));

        return business;
    }

    public void Update(
        string name,
        string? description,
        Location location,
        Guid? placeId = null,
        string? address = null,
        string? city = null,
        string? country = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Business name is required.", nameof(name));

        ArgumentNullException.ThrowIfNull(location);

        Name = name.Trim();
        Description = description?.Trim();
        Address = address?.Trim();
        City = city?.Trim();
        Country = country?.Trim();
        Location = location;
        PlaceId = placeId;

        MarkUpdated();

        AddDomainEvent(new BusinessUpdatedDomainEvent(Id, Name, Description, Address));
    }

    public void UpdateContactInfo(string? phone, string? email, string? website)
    {
        Phone = phone?.Trim();
        Email = email?.Trim();
        Website = website?.Trim();
        MarkUpdated();
    }

    public void UpdateRegistrationDetails(string? licenseNumber, string? taxId)
    {
        LicenseNumber = licenseNumber?.Trim();
        TaxId = taxId?.Trim();
        MarkUpdated();
    }

    public void Approve(Guid reviewedByUserId)
    {
        if (Status != BusinessStatus.Pending)
            throw new InvalidOperationException($"Cannot approve a business with status {Status}.");

        Status = BusinessStatus.Approved;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = DateTime.UtcNow;
        RejectionReason = null;

        MarkUpdated();
        AddDomainEvent(new BusinessApprovedDomainEvent(Id, reviewedByUserId));
    }

    public void Reject(string reason, Guid reviewedByUserId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        if (Status != BusinessStatus.Pending)
            throw new InvalidOperationException($"Cannot reject a business with status {Status}.");

        Status = BusinessStatus.Rejected;
        RejectionReason = reason.Trim();
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = DateTime.UtcNow;

        MarkUpdated();
        AddDomainEvent(new BusinessRejectedDomainEvent(Id, RejectionReason));
    }

    public void Resubmit()
    {
        if (Status != BusinessStatus.Rejected)
            throw new InvalidOperationException($"Cannot resubmit a business with status {Status}.");

        Status = BusinessStatus.Pending;
        RejectionReason = null;

        MarkUpdated();
    }

    public void Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Suspension reason is required.", nameof(reason));

        if (Status != BusinessStatus.Approved)
            throw new InvalidOperationException($"Cannot suspend a business with status {Status}.");

        Status = BusinessStatus.Suspended;

        MarkUpdated();
        AddDomainEvent(new BusinessSuspendedDomainEvent(Id, reason.Trim()));
    }

    public void Reinstate()
    {
        if (Status != BusinessStatus.Suspended)
            throw new InvalidOperationException($"Cannot reinstate a business with status {Status}.");

        Status = BusinessStatus.Approved;

        MarkUpdated();
        AddDomainEvent(new BusinessReinstatedDomainEvent(Id));
    }

    public void SetVerified(bool isVerified)
    {
        IsVerified = isVerified;
        MarkUpdated();
    }

    public void SetFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
        MarkUpdated();
    }

    public void UpdateRating(decimal averageRating, int reviewCount)
    {
        AverageRating = averageRating;
        ReviewCount = reviewCount;
        MarkUpdated();
    }

    /// <summary>
    /// Adds a new translation or updates an existing one for the given language.
    /// Called by domain event handlers — does NOT call SaveChangesAsync.
    /// </summary>
    public void AddOrUpdateTranslation(Guid languageId, string name, string? description)
    {
        var existing = _businessTranslations.FirstOrDefault(t => t.LanguageId == languageId);
        if (existing is not null)
        {
            _businessTranslations.Remove(existing);
        }

        _businessTranslations.Add(BusinessTranslation.Create(Id, languageId, name, description));
    }

    public static string GenerateSlug(string name) =>
        SlugRegex.Replace(name.Trim().ToLowerInvariant().Replace(' ', '-'), string.Empty).Trim('-');
}
