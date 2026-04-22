using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentPlaces.Domain.Entities;

public sealed class Place : AuditableEntity, IAggregateRoot
{
    private readonly List<PlaceTranslation> _placeTranslations = [];

    private Place() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public PlaceType PlaceType { get; private set; }
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
    public bool IsFeatured { get; private set; }
    public bool IsVerified { get; private set; }
    public bool IsWheelchairAccessible { get; private set; }
    public bool HasAudioGuide { get; private set; }
    public bool HasBrailleSignage { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public IReadOnlyCollection<PlaceTranslation> PlaceTranslations => _placeTranslations.AsReadOnly();

    // ── Factory Method (the ONLY way to create) ──────────────────────────────

    public static Place Create(
        string name,
        string slug,
        PlaceType placeType,
        decimal latitude,
        decimal longitude,
        Guid createdByUserId,
        string? description = null,
        string? address = null,
        string? city = null,
        string? country = null,
        string? postalCode = null,
        string? phone = null,
        string? email = null,
        string? website = null,
        string? metaTitle = null,
        string? metaDescription = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Place name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Place slug is required.", nameof(slug));

        var place = new Place
        {
            Name            = name.Trim(),
            Slug            = slug.Trim().ToLowerInvariant(),
            PlaceType       = placeType,
            Location        = new Location(latitude, longitude),
            Description     = description?.Trim(),
            Address         = address?.Trim(),
            City            = city?.Trim(),
            Country         = country?.Trim(),
            PostalCode      = postalCode?.Trim(),
            Phone           = phone?.Trim(),
            Email           = email?.Trim(),
            Website         = website?.Trim(),
            MetaTitle       = metaTitle?.Trim(),
            MetaDescription = metaDescription?.Trim(),
            CreatedByUserId = createdByUserId,
            // Accessibility features default to false (spec requirement)
            IsWheelchairAccessible = false,
            HasAudioGuide          = false,
            HasBrailleSignage      = false,
        };

        place.AddDomainEvent(new PlaceCreatedDomainEvent(place.Id, place.Name, place.Slug));

        return place;
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    public void Update(
        string name,
        string slug,
        PlaceType placeType,
        decimal latitude,
        decimal longitude,
        string? description,
        string? address,
        string? city,
        string? country,
        string? postalCode,
        string? phone,
        string? email,
        string? website,
        string? metaTitle,
        string? metaDescription)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Place name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Place slug is required.", nameof(slug));

        Name            = name.Trim();
        Slug            = slug.Trim().ToLowerInvariant();
        PlaceType       = placeType;
        Location        = new Location(latitude, longitude);
        Description     = description?.Trim();
        Address         = address?.Trim();
        City            = city?.Trim();
        Country         = country?.Trim();
        PostalCode      = postalCode?.Trim();
        Phone           = phone?.Trim();
        Email           = email?.Trim();
        Website         = website?.Trim();
        MetaTitle       = metaTitle?.Trim();
        MetaDescription = metaDescription?.Trim();
        MarkUpdated();

        AddDomainEvent(new PlaceUpdatedDomainEvent(Id, Name, Description, Address));
    }

    public void Delete()
    {
        if (IsDeleted)
            return;

        SoftDelete();
        AddDomainEvent(new PlaceDeletedDomainEvent(Id));
    }

    public void SetFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
        MarkUpdated();
    }

    public void SetVerified(bool isVerified)
    {
        IsVerified = isVerified;
        MarkUpdated();
    }

    public void UpdateRating(decimal averageRating, int reviewCount)
    {
        if (averageRating is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(averageRating), "Rating must be between 0 and 5.");
        if (reviewCount < 0)
            throw new ArgumentOutOfRangeException(nameof(reviewCount), "Review count cannot be negative.");

        AverageRating = averageRating;
        ReviewCount   = reviewCount;
        MarkUpdated();
    }

    public void AddOrUpdateTranslation(Guid languageId, string name, string? description, string? address)
    {
        var existing = _placeTranslations.FirstOrDefault(t => t.LanguageId == languageId);
        if (existing is null)
            _placeTranslations.Add(PlaceTranslation.Create(Id, languageId, name, description, address));
        else
            existing.Update(name, description, address);
    }

    // ── Static Helpers ────────────────────────────────────────────────────────

    /// <summary>Generates a URL-safe slug from a place name (lowercase, hyphens, no special chars).</summary>
    public static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.Trim().ToLowerInvariant().Replace(' ', '-'), @"[^a-z0-9\-]", string.Empty)
            .Trim('-');
}
